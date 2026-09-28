using Microsoft.EntityFrameworkCore;
using MS.SS.Core.Modules.Reception.Domain.Entities;
using MS.SS.Core.Modules.Reception.Domain.Enums;
using MS.SS.Core.Modules.Reception.Domain.Models;
using MS.SS.Core.Modules.Reception.Infrastructure.Repositories;
using MS.SS.Core.Tests.Modules.Reception.Domain;
using MS.SS.Core.Tests.TestHelpers;
using Npgsql;

namespace MS.SS.Core.Tests.Modules.Reception.Infrastructure;

[Collection(PostgresCollection.Name)]
public sealed class DeliveryPersistenceTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Migration_creates_every_table_in_the_reception_schema()
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();

        var tables = await ScalarListAsync(connection,
            "select table_name from information_schema.tables where table_schema = 'reception' order by 1");

        Assert.Equal(new[] { "cartons", "deliveries", "pallets", "product_lines" }, tables.ToArray());
    }

    [Fact]
    public async Task An_aggregate_round_trips_with_quantities_and_packing_list_order()
    {
        var s = new SampleDelivery();
        s.Order.SetPalletValidated(s.Pallet1.Id, true);
        s.Order.SetProductReceivedQuantity(s.C1.Id, 3);
        await using (var write = postgres.CreateContext())
        {
            new DeliveryRepository(write).Add(s.Order);
            await write.SaveChangesAsync();
        }

        await using var read = postgres.CreateContext();
        var loaded = await new DeliveryRepository(read).FindAsync(s.Order.Id, default);

        Assert.NotNull(loaded);
        Assert.Equal(new[] { "PAL-01", "PAL-02" }, loaded.Pallets.OrderBy(p => p.Position).Select(p => p.Code));
        Assert.Equal(new ReceptionProgress(38, 43), loaded.Progress);
        Assert.Equal(ValidationStatus.Partial, loaded.Pallets.Single(p => p.Code == "PAL-02").Status);
    }

    [Fact]
    public async Task A_cascade_on_a_loaded_aggregate_persists_only_through_save()
    {
        var s = new SampleDelivery();
        await using (var seed = postgres.CreateContext())
        {
            new DeliveryRepository(seed).Add(s.Order);
            await seed.SaveChangesAsync();
        }

        await using (var write = postgres.CreateContext())
        {
            var repository = new DeliveryRepository(write);
            var tracked = await repository.FindForUpdateAsync(s.Order.Id, default);
            tracked!.SetCartonValidated(s.CartonA.Id, true);
            await write.SaveChangesAsync();
        }

        await using var read = postgres.CreateContext();
        var reloaded = await new DeliveryRepository(read).FindAsync(s.Order.Id, default);
        Assert.Equal(new ReceptionProgress(15, 43), reloaded!.Progress);
    }

    [Fact]
    public async Task The_database_rejects_a_received_quantity_above_the_expected_quantity()
    {
        var s = new SampleDelivery();
        await using (var write = postgres.CreateContext())
        {
            new DeliveryRepository(write).Add(s.Order);
            await write.SaveChangesAsync();
        }

        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "update reception.product_lines set received_quantity = expected_quantity + 1 where id = @id", connection);
        command.Parameters.AddWithValue("id", s.A1.Id);

        var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal("ck_product_lines_received_bounds", error.ConstraintName);
    }

    [Fact]
    public async Task The_database_rejects_a_non_positive_expected_quantity()
    {
        var s = new SampleDelivery();
        await using (var write = postgres.CreateContext())
        {
            new DeliveryRepository(write).Add(s.Order);
            await write.SaveChangesAsync();
        }

        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "update reception.product_lines set expected_quantity = 0, received_quantity = 0 where id = @id", connection);
        command.Parameters.AddWithValue("id", s.A1.Id);

        var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal("ck_product_lines_expected_positive", error.ConstraintName);
    }

    [Fact]
    public async Task Deleting_a_delivery_removes_its_whole_tree()
    {
        var s = new SampleDelivery();
        await using (var write = postgres.CreateContext())
        {
            new DeliveryRepository(write).Add(s.Order);
            await write.SaveChangesAsync();
        }

        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, "delete from reception.deliveries where id = @id", ("id", s.Order.Id));

        var remaining = await CountAsync(connection,
            "select count(*) from reception.pallets where delivery_id = @id", ("id", s.Order.Id));
        Assert.Equal(0L, remaining);
        Assert.Equal(0L, await CountAsync(connection,
            "select count(*) from reception.product_lines where id = @id", ("id", s.A1.Id)));
    }

    [Fact]
    public async Task Two_deliveries_cannot_share_an_order_number()
    {
        var first = Delivery.Create($"CMD-DUP-{Guid.NewGuid():N}");
        var second = Delivery.Create(first.OrderNumber);
        await using var write = postgres.CreateContext();
        var repository = new DeliveryRepository(write);
        repository.Add(first);
        repository.Add(second);

        await Assert.ThrowsAsync<DbUpdateException>(() => write.SaveChangesAsync());
    }

    [Fact]
    public async Task An_unknown_delivery_id_finds_nothing()
    {
        await using var read = postgres.CreateContext();
        var repository = new DeliveryRepository(read);

        Assert.Null(await repository.FindAsync(Guid.NewGuid(), default));
        Assert.Null(await repository.FindForUpdateAsync(Guid.NewGuid(), default));
    }

    private static async Task<List<string>> ScalarListAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync()) values.Add(reader.GetString(0));
        return values;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, (string Name, object Value) parameter)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountAsync(NpgsqlConnection connection, string sql, (string Name, object Value) parameter)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}

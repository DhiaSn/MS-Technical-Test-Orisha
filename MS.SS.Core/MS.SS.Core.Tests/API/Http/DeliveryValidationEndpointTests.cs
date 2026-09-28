using System.Net;
using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Modules.Reception.Domain.Entities;
using MS.SS.Core.Modules.Reception.Domain.Enums;
using MS.SS.Core.Tests.Modules.Reception.Domain;
using MS.SS.Core.Tests.TestHelpers;

namespace MS.SS.Core.Tests.API.Http;

[Collection(PostgresCollection.Name)]
public sealed class DeliveryValidationEndpointTests : IAsyncLifetime
{
    private readonly ReceptionApiFactory _factory;
    private readonly HttpClient _http;

    public DeliveryValidationEndpointTests(PostgresFixture postgres)
    {
        _factory = new ReceptionApiFactory(postgres);
        _http = _factory.CreateAuthenticatedClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _http.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Validating_a_pallet_marks_all_its_cartons_and_products_received()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);
        var client = new DeliveryClient(_http, s.Order.Id);

        var response = await client.ValidatePalletAsync(s.Pallet1.Id, true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var delivery = (await ApiJson.ReadAsync<DeliveryResponse>(response))!;
        Assert.Equal(ValidationStatus.All, delivery.Pallets[0].Status);
        Assert.All(delivery.Pallets[0].Cartons, c => Assert.Equal(ValidationStatus.All, c.Status));
        Assert.Equal(new ProgressResponse(35, 43), delivery.Progress);
        Assert.Equal(ApiJson.Serialize(delivery), ApiJson.Serialize(await client.GetAsync()));
    }

    [Fact]
    public async Task Validating_a_carton_marks_all_its_products_received()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);

        var delivery = await Read(new DeliveryClient(_http, s.Order.Id).ValidateCartonAsync(s.CartonA.Id, true));

        Assert.Equal(ValidationStatus.All, delivery.Pallets[0].Cartons[0].Status);
        Assert.Equal(ValidationStatus.None, delivery.Pallets[0].Cartons[1].Status);
        Assert.Equal(ValidationStatus.Partial, delivery.Pallets[0].Status);
    }

    [Fact]
    public async Task Ticking_every_product_one_by_one_validates_the_carton_and_unticking_one_makes_the_ancestors_partial()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);
        var client = new DeliveryClient(_http, s.Order.Id);

        await client.ValidateProductAsync(s.A1.Id, true);
        var afterSecond = await Read(client.ValidateProductAsync(s.A2.Id, true));
        Assert.Equal(ValidationStatus.All, afterSecond.Pallets[0].Cartons[0].Status);

        var afterUntick = await Read(client.ValidateProductAsync(s.A1.Id, false));
        Assert.Equal(ValidationStatus.Partial, afterUntick.Pallets[0].Cartons[0].Status);
        Assert.Equal(ValidationStatus.Partial, afterUntick.Pallets[0].Status);
        Assert.Equal(ValidationStatus.Partial, afterUntick.Status);
    }

    [Fact]
    public async Task Unvalidating_a_pallet_resets_everything_under_it()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);
        var client = new DeliveryClient(_http, s.Order.Id);
        await client.ValidatePalletAsync(s.Pallet1.Id, true);

        var delivery = await Read(client.ValidatePalletAsync(s.Pallet1.Id, false));

        Assert.Equal(new ProgressResponse(0, 43), delivery.Progress);
        Assert.Equal(ValidationStatus.None, delivery.Status);
    }

    [Fact]
    public async Task A_typed_quantity_makes_the_product_partial_and_the_progress_counts_units()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);

        var delivery = await Read(new DeliveryClient(_http, s.Order.Id).SetQuantityAsync(s.A1.Id, 4));

        var product = delivery.Pallets[0].Cartons[0].Products[0];
        Assert.Equal((4, ValidationStatus.Partial), (product.ReceivedQuantity, product.Status));
        Assert.Equal(new ProgressResponse(4, 43), delivery.Progress);
    }

    [Fact]
    public async Task Repeating_the_same_request_returns_an_identical_body()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);
        var client = new DeliveryClient(_http, s.Order.Id);

        var first = await Read(client.ValidateCartonAsync(s.CartonA.Id, true));
        var second = await Read(client.ValidateCartonAsync(s.CartonA.Id, true));

        Assert.Equal(ApiJson.Serialize(first), ApiJson.Serialize(second));
    }

    [Fact]
    public async Task Concurrent_requests_on_one_delivery_all_succeed_and_end_consistent()
    {
        var order = Delivery.Create($"CMD-CONC-{Guid.NewGuid():N}");
        var carton = order.AddPallet("PAL-01").AddCarton("CART-01");
        var products = Enumerable.Range(1, 12)
            .Select(i => carton.AddProduct($"REF-{i:00}", $"Item {i}", "Rouge", "M", 5))
            .ToList();
        await _factory.SeedAsync(order);
        var client = new DeliveryClient(_http, order.Id);

        var responses = await Task.WhenAll(products.Select(p => client.ValidateProductAsync(p.Id, true)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var final = await client.GetAsync();
        Assert.Equal(ValidationStatus.All, final.Status);
        Assert.Equal(new ProgressResponse(60, 60), final.Progress);
    }

    private static async Task<DeliveryResponse> Read(Task<HttpResponseMessage> pending)
    {
        var response = await pending;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await ApiJson.ReadAsync<DeliveryResponse>(response))!;
    }
}

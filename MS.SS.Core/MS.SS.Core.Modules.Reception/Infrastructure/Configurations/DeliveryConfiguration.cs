using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MS.SS.Core.Infrastructure.Database.Config;
using MS.SS.Core.Modules.Reception.Domain.Entities;

namespace MS.SS.Core.Modules.Reception.Infrastructure.Configurations;

public sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("deliveries", ModuleSchemas.Reception);

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.OrderNumber).IsRequired().HasMaxLength(50);
        builder.Property(d => d.CreatedAt).IsRequired();

        builder.HasIndex(d => d.OrderNumber).IsUnique().HasDatabaseName("ux_deliveries_order_number");

        builder.Ignore(d => d.Progress);
        builder.Ignore(d => d.Status);

        builder.HasMany(d => d.Pallets)
            .WithOne()
            .HasForeignKey("DeliveryId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(d => d.Pallets).HasField("_pallets").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

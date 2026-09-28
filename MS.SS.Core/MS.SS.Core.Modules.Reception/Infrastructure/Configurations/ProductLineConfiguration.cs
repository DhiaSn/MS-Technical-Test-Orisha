using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MS.SS.Core.Infrastructure.Database.Config;
using MS.SS.Core.Modules.Reception.Domain.Entities;

namespace MS.SS.Core.Modules.Reception.Infrastructure.Configurations;

public sealed class ProductLineConfiguration : IEntityTypeConfiguration<ProductLine>
{
    public void Configure(EntityTypeBuilder<ProductLine> builder)
    {
        builder.ToTable("product_lines", ModuleSchemas.Reception, table =>
        {
            table.HasCheckConstraint("ck_product_lines_expected_positive", "expected_quantity > 0");
            table.HasCheckConstraint(
                "ck_product_lines_received_bounds",
                "received_quantity >= 0 AND received_quantity <= expected_quantity");
        });

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.Reference).IsRequired().HasMaxLength(50);
        builder.Property(l => l.Name).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Color).IsRequired().HasMaxLength(50);
        builder.Property(l => l.Size).IsRequired().HasMaxLength(20);
        builder.Property(l => l.ExpectedQuantity).IsRequired();
        builder.Property(l => l.ReceivedQuantity).IsRequired().HasDefaultValue(0);
        builder.Property(l => l.Position).IsRequired();

        builder.Property<Guid>("CartonId");
        builder.HasIndex("CartonId", nameof(ProductLine.Reference))
            .IsUnique()
            .HasDatabaseName("ux_product_lines_carton_reference");

        builder.Ignore(l => l.Progress);
        builder.Ignore(l => l.Status);
    }
}

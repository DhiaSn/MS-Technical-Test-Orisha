using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MS.SS.Core.Infrastructure.Database.Config;
using MS.SS.Core.Modules.Reception.Domain.Entities;

namespace MS.SS.Core.Modules.Reception.Infrastructure.Configurations;

public sealed class PalletConfiguration : IEntityTypeConfiguration<Pallet>
{
    public void Configure(EntityTypeBuilder<Pallet> builder)
    {
        builder.ToTable("pallets", ModuleSchemas.Reception);

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Position).IsRequired();

        // Declared here and wired by DeliveryConfiguration, so the index does not depend on which
        // configuration EF applies first.
        builder.Property<Guid>("DeliveryId");
        builder.HasIndex("DeliveryId", nameof(Pallet.Code)).IsUnique().HasDatabaseName("ux_pallets_delivery_code");

        builder.Ignore(p => p.Progress);
        builder.Ignore(p => p.Status);

        builder.HasMany(p => p.Cartons)
            .WithOne()
            .HasForeignKey("PalletId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Cartons).HasField("_cartons").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MS.SS.Core.Infrastructure.Database.Config;
using MS.SS.Core.Modules.Reception.Domain.Entities;

namespace MS.SS.Core.Modules.Reception.Infrastructure.Configurations;

public sealed class CartonConfiguration : IEntityTypeConfiguration<Carton>
{
    public void Configure(EntityTypeBuilder<Carton> builder)
    {
        builder.ToTable("cartons", ModuleSchemas.Reception);

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Position).IsRequired();

        builder.Property<Guid>("PalletId");
        builder.HasIndex("PalletId", nameof(Carton.Code)).IsUnique().HasDatabaseName("ux_cartons_pallet_code");

        builder.Ignore(c => c.Progress);
        builder.Ignore(c => c.Status);

        builder.HasMany(c => c.Products)
            .WithOne()
            .HasForeignKey("CartonId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Products).HasField("_products").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

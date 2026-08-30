using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class ApartmentChargeHistoryConfiguration : IEntityTypeConfiguration<ApartmentChargeHistory>
{
    public void Configure(EntityTypeBuilder<ApartmentChargeHistory> builder)
    {
        builder.ToTable("apartment_charge_history");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ApartmentId)
            .HasColumnName("apartment_id")
            .IsRequired();

        builder.Property(x => x.ChargeType)
            .HasColumnName("charge_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(x => x.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.EffectiveTo)
            .HasColumnName("effective_to")
            .HasColumnType("date");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(x => x.Apartment)
            .WithMany()
            .HasForeignKey(x => x.ApartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Used by the charge-history endpoint to walk the timeline per apartment/charge type
        builder.HasIndex(x => new { x.ApartmentId, x.ChargeType, x.EffectiveFrom });
    }
}

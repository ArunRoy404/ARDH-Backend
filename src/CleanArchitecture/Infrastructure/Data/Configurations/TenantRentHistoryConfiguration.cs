using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class TenantRentHistoryConfiguration : IEntityTypeConfiguration<TenantRentHistory>
{
    public void Configure(EntityTypeBuilder<TenantRentHistory> builder)
    {
        builder.ToTable("tenant_rent_history");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.MonthlyRent)
            .HasColumnName("monthly_rent")
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

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Used by the occupancy report to walk the rent timeline for a lease
        builder.HasIndex(x => new { x.TenantId, x.EffectiveFrom });
    }
}

using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class ApartmentConfiguration : IEntityTypeConfiguration<Apartment>
{
    public void Configure(EntityTypeBuilder<Apartment> builder)
    {
        builder.ToTable("apartments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.BuildingId)
            .HasColumnName("building_id")
            .IsRequired();

        builder.Property(e => e.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(e => e.NestawayId)
            .HasColumnName("nestaway_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.FlatNumber)
            .HasColumnName("flat_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Floor)
            .HasColumnName("floor")
            .IsRequired();

        builder.Property(e => e.ApartmentType)
            .HasColumnName("apartment_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.AreaSqft)
            .HasColumnName("area_sqft")
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(e => e.Bedrooms)
            .HasColumnName("bedrooms")
            .IsRequired();

        builder.Property(e => e.Bathrooms)
            .HasColumnName("bathrooms")
            .IsRequired();

        builder.Property(e => e.HasBalcony)
            .HasColumnName("has_balcony")
            .IsRequired();

        builder.Property(e => e.ParkingSlot)
            .HasColumnName("parking_slot")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ExpectedRent)
            .HasColumnName("expected_rent")
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(e => e.MaintenanceCharge)
            .HasColumnName("maintenance_charge")
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(e => e.WaterCharge)
            .HasColumnName("water_charge")
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(e => e.CurrentTenantId)
            .HasColumnName("current_tenant_id");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        // Relationships
        builder.HasOne(e => e.Building)
            .WithMany()
            .HasForeignKey(e => e.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Owner)
            .WithMany()
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

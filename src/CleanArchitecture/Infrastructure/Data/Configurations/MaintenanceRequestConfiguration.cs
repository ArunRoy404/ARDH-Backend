using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.ToTable("maintenance_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.VendorId)
            .HasColumnName("vendor_id");

        builder.Property(x => x.EquipmentId)
            .HasColumnName("equipment_id");

        builder.Property(x => x.BuildingId)
            .HasColumnName("building_id")
            .IsRequired();

        builder.Property(x => x.ApartmentId)
            .HasColumnName("apartment_id");

        builder.Property(x => x.EstimatedCost)
            .HasColumnName("estimated_cost")
            .HasColumnType("decimal(14,2)")
            .IsRequired();

        builder.Property(x => x.AnnualCost)
            .HasColumnName("annual_cost")
            .HasColumnType("decimal(14,2)")
            .IsRequired();

        builder.Property(x => x.ScheduledDate)
            .HasColumnName("scheduled_date");

        builder.Property(x => x.ReceiptAttachmentUrl)
            .HasColumnName("receipt_attachment_url")
            .HasMaxLength(1000);

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(x => x.DeletedBy)
            .HasColumnName("deleted_by");

        builder.Property(x => x.RestoredBy)
            .HasColumnName("restored_by");

        builder.HasOne(x => x.Building)
            .WithMany()
            .HasForeignKey(x => x.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Apartment)
            .WithMany()
            .HasForeignKey(x => x.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vendor)
            .WithMany()
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Equipment)
            .WithMany()
            .HasForeignKey(x => x.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

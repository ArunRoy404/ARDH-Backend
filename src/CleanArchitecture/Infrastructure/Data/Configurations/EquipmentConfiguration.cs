using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("equipment");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.BuildingId)
            .HasColumnName("building_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Brand)
            .HasColumnName("brand")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Model)
            .HasColumnName("model")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.SerialNumber)
            .HasColumnName("serial_number")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.InstallDate)
            .HasColumnName("install_date")
            .IsRequired();

        builder.Property(x => x.WarrantyExpiryDate)
            .HasColumnName("warranty_expiry_date")
            .IsRequired();

        builder.Property(x => x.AmcVendorId)
            .HasColumnName("amc_vendor_id")
            .IsRequired();

        builder.Property(x => x.AmcExpiryDate)
            .HasColumnName("amc_expiry_date")
            .IsRequired();

        builder.Property(x => x.LastServiceDate)
            .HasColumnName("last_service_date")
            .IsRequired();

        builder.Property(x => x.NextServiceDate)
            .HasColumnName("next_service_date")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.AttachmentUrl)
            .HasColumnName("attachment_url")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasOne(x => x.Building)
            .WithMany()
            .HasForeignKey(x => x.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AmcVendor)
            .WithMany()
            .HasForeignKey(x => x.AmcVendorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

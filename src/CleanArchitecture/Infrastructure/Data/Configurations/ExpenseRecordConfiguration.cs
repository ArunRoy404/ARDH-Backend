using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class ExpenseRecordConfiguration : IEntityTypeConfiguration<ExpenseRecord>
{
    public void Configure(EntityTypeBuilder<ExpenseRecord> builder)
    {
        builder.ToTable("expense_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ExpenseHead)
            .HasColumnName("expense_head")
            .HasMaxLength(255);

        builder.Property(x => x.SpecificItem)
            .HasColumnName("specific_item")
            .HasMaxLength(255);

        builder.Property(x => x.VendorId)
            .HasColumnName("vendor_id");

        builder.Property(x => x.Nature)
            .HasColumnName("nature")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(14,2)")
            .IsRequired();

        builder.Property(x => x.Entity)
            .HasColumnName("entity")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.BuildingId)
            .HasColumnName("building_id");

        builder.Property(x => x.ApartmentId)
            .HasColumnName("apartment_id");

        builder.Property(x => x.ExpenseDate)
            .HasColumnName("expense_date")
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasColumnName("payment_method")
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Reference)
            .HasColumnName("reference")
            .HasMaxLength(255);

        builder.Property(x => x.AttachmentUrl)
            .HasColumnName("attachment_url")
            .HasMaxLength(1000);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("nvarchar(max)");

        // Water Tank Delivery Fields
        builder.Property(x => x.TankerNumber)
            .HasColumnName("tanker_number")
            .HasMaxLength(100);

        builder.Property(x => x.TimeOfDelivery)
            .HasColumnName("time_of_delivery");

        builder.Property(x => x.DeliveryDriverName)
            .HasColumnName("delivery_driver_name")
            .HasMaxLength(255);

        builder.Property(x => x.ManagerInAttendance)
            .HasColumnName("manager_in_attendance")
            .HasMaxLength(255);

        builder.Property(x => x.LitersFilled)
            .HasColumnName("liters_filled");

        // Audit Fields
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

        // Navigation Properties
        builder.HasOne(x => x.Vendor)
            .WithMany()
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Building)
            .WithMany()
            .HasForeignKey(x => x.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Apartment)
            .WithMany()
            .HasForeignKey(x => x.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

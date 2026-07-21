using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class IncomeRecordConfiguration : IEntityTypeConfiguration<IncomeRecord>
{
    public void Configure(EntityTypeBuilder<IncomeRecord> builder)
    {
        builder.ToTable("income_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.IncomeEntity)
            .HasColumnName("income_entity")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IncomeType)
            .HasColumnName("income_type")
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(14,2)")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(x => x.BuildingId)
            .HasColumnName("building_id");

        builder.Property(x => x.ApartmentId)
            .HasColumnName("apartment_id");

        builder.Property(x => x.Period)
            .HasColumnName("period")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PaymentDate)
            .HasColumnName("payment_date")
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasColumnName("payment_method")
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TransactionReference)
            .HasColumnName("transaction_reference")
            .HasMaxLength(255);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.AttachmentUrl)
            .HasColumnName("attachment_url")
            .HasMaxLength(1000);

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

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
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

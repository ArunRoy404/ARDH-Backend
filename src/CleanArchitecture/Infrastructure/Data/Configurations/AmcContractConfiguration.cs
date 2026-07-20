using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class AmcContractConfiguration : IEntityTypeConfiguration<AmcContract>
{
    public void Configure(EntityTypeBuilder<AmcContract> builder)
    {
        builder.ToTable("amc_contracts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.AmcCode)
            .HasColumnName("amc_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.AmcCode)
            .IsUnique();

        builder.Property(x => x.ContractNumber)
            .HasColumnName("contract_number")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ContractTitle)
            .HasColumnName("contract_title")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ContractType)
            .HasColumnName("contract_type")
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EquipmentId)
            .HasColumnName("equipment_id")
            .IsRequired();

        builder.Property(x => x.VendorId)
            .HasColumnName("vendor_id")
            .IsRequired();

        builder.Property(x => x.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(x => x.EndDate)
            .HasColumnName("end_date")
            .IsRequired();

        builder.Property(x => x.ContractAmount)
            .HasColumnName("contract_amount")
            .HasColumnType("decimal(14,2)")
            .IsRequired();

        builder.Property(x => x.PaymentTerms)
            .HasColumnName("payment_terms")
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ServiceFrequency)
            .HasColumnName("service_frequency")
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CoverageDetails)
            .HasColumnName("coverage_details")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.Exclusions)
            .HasColumnName("exclusions")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.DocumentLink)
            .HasColumnName("document_link")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Remarks)
            .HasColumnName("remarks")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

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

        builder.HasOne(x => x.Equipment)
            .WithMany()
            .HasForeignKey(x => x.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vendor)
            .WithMany()
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

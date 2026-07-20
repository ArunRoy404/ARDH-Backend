using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.BuildingId)
            .HasColumnName("building_id")
            .IsRequired();

        builder.Property(x => x.ApartmentId)
            .HasColumnName("apartment_id")
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.IdType)
            .HasColumnName("id_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IdNumber)
            .HasColumnName("id_number")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IdProofAttachmentUrl)
            .HasColumnName("id_proof_attachment_url")
            .HasMaxLength(1000);

        builder.Property(x => x.MoveInDate)
            .HasColumnName("move_in_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.LeaseStartDate)
            .HasColumnName("lease_start_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.LeaseEndDate)
            .HasColumnName("lease_end_date")
            .HasColumnType("date");

        builder.Property(x => x.MonthlyRent)
            .HasColumnName("monthly_rent")
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(x => x.SecurityDeposit)
            .HasColumnName("security_deposit")
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(x => x.EmergencyContactName)
            .HasColumnName("emergency_contact_name")
            .HasMaxLength(255);

        builder.Property(x => x.EmergencyContactPhone)
            .HasColumnName("emergency_contact_phone")
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

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

        // Foreign keys
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

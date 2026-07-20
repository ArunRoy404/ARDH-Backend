using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class TenantMoveOutRecordConfiguration : IEntityTypeConfiguration<TenantMoveOutRecord>
{
    public void Configure(EntityTypeBuilder<TenantMoveOutRecord> builder)
    {
        builder.ToTable("tenant_move_out_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.ApartmentId)
            .HasColumnName("apartment_id")
            .IsRequired();

        builder.Property(x => x.MoveOutDate)
            .HasColumnName("move_out_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.PendingRent)
            .HasColumnName("pending_rent")
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(x => x.DamageAmount)
            .HasColumnName("damage_amount")
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(x => x.RefundAmount)
            .HasColumnName("refund_amount")
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(x => x.IdNumber)
            .HasColumnName("id_number")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.HandoverNote)
            .HasColumnName("handover_note")
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.ProcessedBy)
            .HasColumnName("processed_by")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at");

        // Foreign keys
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Apartment)
            .WithMany()
            .HasForeignKey(x => x.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Processor)
            .WithMany()
            .HasForeignKey(x => x.ProcessedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

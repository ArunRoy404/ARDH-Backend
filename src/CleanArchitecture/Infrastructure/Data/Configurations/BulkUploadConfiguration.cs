using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class BulkUploadConfiguration : IEntityTypeConfiguration<BulkUpload>
{
    public void Configure(EntityTypeBuilder<BulkUpload> builder)
    {
        builder.ToTable("bulk_uploads");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Module)
            .HasColumnName("module")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.OriginalFileUrl)
            .HasColumnName("original_file_url")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.ProcessedFileUrl)
            .HasColumnName("processed_file_url")
            .HasMaxLength(1000);

        builder.Property(x => x.TotalCount)
            .HasColumnName("total_count")
            .IsRequired();

        builder.Property(x => x.SuccessCount)
            .HasColumnName("success_count")
            .IsRequired();

        builder.Property(x => x.FailedCount)
            .HasColumnName("failed_count")
            .IsRequired();

        builder.Property(x => x.GlobalError)
            .HasColumnName("global_error")
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(x => x.FinishedAt)
            .HasColumnName("finished_at");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by");

        // Index for fast "list by module" queries
        builder.HasIndex(x => new { x.Module, x.CreatedAt });
    }
}

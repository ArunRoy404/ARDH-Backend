using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class DeletedHistoryConfiguration : IEntityTypeConfiguration<DeletedHistory>
{
    public void Configure(EntityTypeBuilder<DeletedHistory> builder)
    {
        builder.ToTable("deleted_history");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EntityId)
            .IsRequired();

        builder.Property(x => x.EntityTitle)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.DeletedAt)
            .IsRequired();
    }
}

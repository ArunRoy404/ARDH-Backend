using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("activities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActionType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.BuildingId);
        builder.Property(x => x.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Building)
            .WithMany()
            .HasForeignKey(x => x.BuildingId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

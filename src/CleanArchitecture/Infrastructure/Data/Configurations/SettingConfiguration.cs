using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("settings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.CompanyEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Address)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Icon)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Fav)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.AdminPassword)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.UpdatedBy);

        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}

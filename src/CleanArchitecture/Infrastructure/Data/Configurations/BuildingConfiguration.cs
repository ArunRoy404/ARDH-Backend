using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("buildings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BuildingName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Address)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.State)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.GoogleMapLink)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.TotalFloors)
            .IsRequired();

        builder.Property(x => x.ParkingDetails)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.ImageUrl)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property(x => x.DeletedAt);
    }
}

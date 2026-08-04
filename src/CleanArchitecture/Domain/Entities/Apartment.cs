using System;

namespace CleanArchitecture.Domain.Entities;

public class Apartment
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public Guid OwnerId { get; set; }
    public string NestawayId { get; set; } = null!;
    public string FlatNumber { get; set; } = null!;
    public int Floor { get; set; }
    public string ApartmentType { get; set; } = null!;
    public decimal AreaSqft { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public bool HasBalcony { get; set; }
    public string ParkingSlot { get; set; } = null!;
    public decimal ExpectedRent { get; set; }
    public decimal MaintenanceCharge { get; set; }
    public decimal WaterCharge { get; set; }
    public string? Notes { get; set; }
    public Guid? CurrentTenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public Building Building { get; set; } = null!;
    public Owner Owner { get; set; } = null!;
}
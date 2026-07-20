using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Apartment;

public class ApartmentUpdateRequest
{
    public Guid BuildingId { get; set; }
    public Guid OwnerId { get; set; }
    public string NestawayId { get; set; } = null!;
    public string FlatNumber { get; set; } = null!;
    public int Floor { get; set; }
    public ApartmentType ApartmentType { get; set; }
    public decimal AreaSqft { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public bool HasBalcony { get; set; }
    public string ParkingSlot { get; set; } = null!;
    public decimal ExpectedRent { get; set; }
    public decimal MaintenanceCharge { get; set; }
    public decimal WaterCharge { get; set; }
}

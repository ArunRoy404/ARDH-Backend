using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class Building
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BuildingName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string GoogleMapLink { get; set; } = string.Empty;
    public int TotalFloors { get; set; }
    public string ParkingDetails { get; set; } = string.Empty;
    public BuildingStatus Status { get; set; } = BuildingStatus.active;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}

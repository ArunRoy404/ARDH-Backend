using System;
using System.ComponentModel.DataAnnotations;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Building;

public class BuildingUpdateRequest
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string BuildingName { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required]
    public string Country { get; set; } = string.Empty;

    [Required]
    public string GoogleMapLink { get; set; } = string.Empty;

    [Required]
    public int TotalFloors { get; set; }

    [Required]
    public string ParkingDetails { get; set; } = string.Empty;

    [Required]
    public BuildingStatus Status { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string ImageUrl { get; set; } = string.Empty;
}

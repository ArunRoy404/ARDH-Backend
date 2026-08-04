using System.ComponentModel.DataAnnotations;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Building;

public class BuildingCreateRequest
{
    [Required]
    public string BuildingName { get; set; } = string.Empty;

    public string? Address { get; set; }

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required]
    public string Country { get; set; } = string.Empty;

    public string? GoogleMapLink { get; set; }

    public int? TotalFloors { get; set; }

    public string? ParkingDetails { get; set; }

    public BuildingStatus? Status { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }
}

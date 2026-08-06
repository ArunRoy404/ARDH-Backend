using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Equipment;

public class EquipmentUpdateRequest
{
    public Guid BuildingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime InstallDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public EquipmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentUrl { get; set; }
}

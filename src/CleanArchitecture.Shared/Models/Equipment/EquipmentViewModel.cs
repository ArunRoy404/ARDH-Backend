using System;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Building;
using CleanArchitecture.Shared.Models.Vendor;

namespace CleanArchitecture.Shared.Models.Equipment;

public class EquipmentViewModel
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public BuildingViewModel? Building { get; set; }
    public string Name { get; set; } = string.Empty;
    public EquipmentType Type { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime InstallDate { get; set; }
    public DateTime WarrantyExpiryDate { get; set; }
    public Guid AmcVendorId { get; set; }
    public string AmcVendorName { get; set; } = string.Empty;
    public string AmcVendorCompanyName { get; set; } = string.Empty;
    public VendorViewModel? AmcVendor { get; set; }
    public DateTime AmcExpiryDate { get; set; }
    public DateTime LastServiceDate { get; set; }
    public DateTime NextServiceDate { get; set; }
    public EquipmentStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string AttachmentUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
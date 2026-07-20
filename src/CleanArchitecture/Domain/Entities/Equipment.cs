using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class Equipment
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public string Name { get; set; } = null!;
    public EquipmentType Type { get; set; }
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string SerialNumber { get; set; } = null!;
    public DateTime InstallDate { get; set; }
    public DateTime WarrantyExpiryDate { get; set; }
    public Guid AmcVendorId { get; set; }
    public DateTime AmcExpiryDate { get; set; }
    public DateTime LastServiceDate { get; set; }
    public DateTime NextServiceDate { get; set; }
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Operational;
    public string Notes { get; set; } = null!;
    public string AttachmentUrl { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public virtual Building? Building { get; set; }
    public virtual Vendor? AmcVendor { get; set; }
}

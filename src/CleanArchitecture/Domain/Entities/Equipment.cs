using System;

namespace CleanArchitecture.Domain.Entities;

public class Equipment
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime InstallDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public string Status { get; set; } = "Operational";
    public string? Notes { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;

    public virtual Building? Building { get; set; }
}

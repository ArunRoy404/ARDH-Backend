using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class MaintenanceRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public MaintenanceCategory Category { get; set; }
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;
    public Guid? VendorId { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid BuildingId { get; set; }
    public Guid? ApartmentId { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal AnnualCost { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? ReceiptAttachmentUrl { get; set; }
    public string? Notes { get; set; }
    
    // Audit Fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid? RestoredBy { get; set; }

    // Navigation Properties
    public virtual Building? Building { get; set; }
    public virtual Apartment? Apartment { get; set; }
    public virtual Vendor? Vendor { get; set; }
    public virtual Equipment? Equipment { get; set; }
}

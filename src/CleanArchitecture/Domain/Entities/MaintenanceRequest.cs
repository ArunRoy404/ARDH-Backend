using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class MaintenanceRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Category { get; set; } = string.Empty;
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;
    public Guid? VendorId { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid BuildingId { get; set; }
    public Guid? ApartmentId { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal AnnualCost { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? StartDate { get; set; }
    public MaintenanceRecurrenceFrequency? RecurrenceFrequency { get; set; }
    public string? ReceiptAttachmentUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime? LastCompletedDate { get; set; }

    /// <summary>
    /// Set once the recurring reminder job has already spawned this request's successor, so the
    /// nightly scan doesn't keep re-evaluating (and re-checking for duplicates against) the same
    /// completed occurrence forever.
    /// </summary>
    public bool NextCycleGenerated { get; set; } = false;

    // Audit Fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation Properties
    public virtual Building? Building { get; set; }
    public virtual Apartment? Apartment { get; set; }
    public virtual Vendor? Vendor { get; set; }
    public virtual Equipment? Equipment { get; set; }
}
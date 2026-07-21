using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Maintenance;

public class MaintenanceRequestViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public MaintenanceCategory Category { get; set; }
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public string? VendorCompanyName { get; set; }
    public Guid? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public Guid BuildingId { get; set; }
    public string BuildingName { get; set; } = null!;
    public Guid? ApartmentId { get; set; }
    public string? FlatNumber { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal AnnualCost { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? ReceiptAttachmentUrl { get; set; }
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid? RestoredBy { get; set; }
}

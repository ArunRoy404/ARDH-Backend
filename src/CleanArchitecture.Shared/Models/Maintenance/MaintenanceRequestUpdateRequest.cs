using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Maintenance;

public class MaintenanceRequestUpdateRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public MaintenanceCategory Category { get; set; }
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public Guid BuildingId { get; set; }
    public Guid? ApartmentId { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? EquipmentId { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal AnnualCost { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? ReceiptAttachmentUrl { get; set; }
    public string? Notes { get; set; }
}

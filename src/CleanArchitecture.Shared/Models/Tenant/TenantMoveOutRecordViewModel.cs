using System;

namespace CleanArchitecture.Shared.Models.Tenant;

public class TenantMoveOutRecordViewModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = null!;
    public Guid ApartmentId { get; set; }
    public string FlatNumber { get; set; } = null!;
    public DateTime MoveOutDate { get; set; }
    public decimal PendingRent { get; set; }
    public decimal DamageAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string IdNumber { get; set; } = null!;
    public string? HandoverNote { get; set; }
    public Guid ProcessedBy { get; set; }
    public string ProcessedByName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

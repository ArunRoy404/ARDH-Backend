using System;
using CleanArchitecture.Shared.Models.Apartment;

namespace CleanArchitecture.Shared.Models.Tenant;

public class TenantMoveOutRecordViewModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = null!;
    public TenantViewModel? Tenant { get; set; }
    public Guid ApartmentId { get; set; }
    public string FlatNumber { get; set; } = null!;
    public ApartmentViewModel? Apartment { get; set; }
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
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
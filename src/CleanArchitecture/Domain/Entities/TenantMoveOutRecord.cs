using System;

namespace CleanArchitecture.Domain.Entities;

public class TenantMoveOutRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ApartmentId { get; set; }
    public DateTime MoveOutDate { get; set; }
    public decimal PendingRent { get; set; }
    public decimal DamageAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string IdNumber { get; set; } = null!;
    public string? HandoverNote { get; set; }
    public Guid ProcessedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public Apartment Apartment { get; set; } = null!;
    public User Processor { get; set; } = null!;
}

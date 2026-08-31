using System;

namespace CleanArchitecture.Domain.Entities;

public class TenantRentHistory
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public decimal MonthlyRent { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
}

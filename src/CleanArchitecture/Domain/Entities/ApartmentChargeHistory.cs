using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class ApartmentChargeHistory
{
    public Guid Id { get; set; }
    public Guid ApartmentId { get; set; }
    public ApartmentChargeType ChargeType { get; set; }
    public decimal Amount { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Apartment Apartment { get; set; } = null!;
}

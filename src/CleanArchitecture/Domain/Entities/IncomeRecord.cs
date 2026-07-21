using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class IncomeRecord
{
    public Guid Id { get; set; }
    public IncomeEntity IncomeEntity { get; set; }
    public IncomeType IncomeType { get; set; }
    public decimal Amount { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? BuildingId { get; set; }
    public Guid? ApartmentId { get; set; }
    public string Period { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public IncomePaymentMethod PaymentMethod { get; set; }
    public string? TransactionReference { get; set; }
    public IncomeStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentUrl { get; set; }

    // Audit Fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid? RestoredBy { get; set; }

    // Navigation Properties
    public virtual Tenant? Tenant { get; set; }
    public virtual Building? Building { get; set; }
    public virtual Apartment? Apartment { get; set; }
}

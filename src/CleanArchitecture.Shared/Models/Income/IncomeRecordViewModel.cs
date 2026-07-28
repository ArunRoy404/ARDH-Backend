using System;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Tenant;
using CleanArchitecture.Shared.Models.Building;
using CleanArchitecture.Shared.Models.Apartment;

namespace CleanArchitecture.Shared.Models.Income;

public class IncomeRecordViewModel
{
    public Guid Id { get; set; }
    public IncomeEntity IncomeEntity { get; set; }
    public IncomeType IncomeType { get; set; }
    public decimal Amount { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public TenantViewModel? Tenant { get; set; }
    public Guid? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public BuildingViewModel? Building { get; set; }
    public Guid? ApartmentId { get; set; }
    public string? FlatNumber { get; set; }
    public ApartmentViewModel? Apartment { get; set; }
    public string Period { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public IncomePaymentMethod PaymentMethod { get; set; }
    public string? TransactionReference { get; set; }
    public IncomeStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
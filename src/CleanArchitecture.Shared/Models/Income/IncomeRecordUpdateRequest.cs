using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Income;

public class IncomeRecordUpdateRequest
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
}

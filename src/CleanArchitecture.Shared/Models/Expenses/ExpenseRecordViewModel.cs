using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Expenses;

public class ExpenseRecordViewModel
{
    public Guid Id { get; set; }
    public ExpenseCategory Category { get; set; }
    public string? ExpenseHead { get; set; }
    public string? SpecificItem { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public string? VendorCompanyName { get; set; }
    public ExpenseNature Nature { get; set; }
    public decimal Amount { get; set; }
    public ExpenseEntity Entity { get; set; }
    public Guid? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public Guid? ApartmentId { get; set; }
    public string? FlatNumber { get; set; }
    public DateTime ExpenseDate { get; set; }
    public ExpensePaymentMethod PaymentMethod { get; set; }
    public ExpenseStatus Status { get; set; }
    public string? Reference { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? Description { get; set; }

    // Water Tank Delivery fields
    public string? TankerNumber { get; set; }
    public DateTime? TimeOfDelivery { get; set; }
    public string? DeliveryDriverName { get; set; }
    public string? ManagerInAttendance { get; set; }
    public int? LitersFilled { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid? RestoredBy { get; set; }
}

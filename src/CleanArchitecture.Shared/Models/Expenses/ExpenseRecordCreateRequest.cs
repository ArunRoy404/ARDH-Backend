using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Expenses;

public class ExpenseRecordCreateRequest
{
    public ExpenseCategory Category { get; set; }
    public string? ExpenseHead { get; set; }
    public string? SpecificItem { get; set; }
    public Guid? VendorId { get; set; }
    public ExpenseNature Nature { get; set; }
    public decimal Amount { get; set; }
    public ExpenseEntity Entity { get; set; }
    public Guid? BuildingId { get; set; }
    public Guid? ApartmentId { get; set; }
    public DateTime ExpenseDate { get; set; }
    public ExpensePaymentMethod PaymentMethod { get; set; }
    public ExpenseStatus Status { get; set; }
    public string? Reference { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? Description { get; set; }

    // Water Tank fields
    public string? TankerNumber { get; set; }
    public DateTime? TimeOfDelivery { get; set; }
    public string? DeliveryDriverName { get; set; }
    public string? ManagerInAttendance { get; set; }
    public int? LitersFilled { get; set; }
}

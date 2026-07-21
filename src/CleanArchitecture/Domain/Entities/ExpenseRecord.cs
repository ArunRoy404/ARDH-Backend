using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class ExpenseRecord
{
    public Guid Id { get; set; }
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

    // Water Tank Delivery fields
    public string? TankerNumber { get; set; }
    public DateTime? TimeOfDelivery { get; set; }
    public string? DeliveryDriverName { get; set; }
    public string? ManagerInAttendance { get; set; }
    public int? LitersFilled { get; set; }

    // Audit Fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid? RestoredBy { get; set; }

    // Navigation Properties
    public virtual Vendor? Vendor { get; set; }
    public virtual Building? Building { get; set; }
    public virtual Apartment? Apartment { get; set; }
}

using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public Guid ApartmentId { get; set; }
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public OwnerIdType IdType { get; set; }
    public string IdNumber { get; set; } = null!;
    public string? IdProofAttachmentUrl { get; set; }
    public DateTime MoveInDate { get; set; }
    public DateTime LeaseStartDate { get; set; }
    public DateTime? LeaseEndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Building Building { get; set; } = null!;
    public Apartment Apartment { get; set; } = null!;
}

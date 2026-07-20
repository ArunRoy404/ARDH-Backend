using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Tenant;

public class TenantViewModel
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public string BuildingName { get; set; } = null!;
    public Guid ApartmentId { get; set; }
    public string FlatNumber { get; set; } = null!;
    public string? NestawayId { get; set; }
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
    public TenantStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

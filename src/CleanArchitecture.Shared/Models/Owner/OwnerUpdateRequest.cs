using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Owner;

public class OwnerUpdateRequest
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Address { get; set; } = null!;
    public OwnerIdType IdType { get; set; }
    public string IdNumber { get; set; } = null!;
    public string BankName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string IfscCode { get; set; } = null!;
    public OwnerStatus Status { get; set; }
    public string? Notes { get; set; }
}

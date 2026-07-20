using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class Vendor
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string CompanyName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public VendorType VendorType { get; set; }
    public string GstNumber { get; set; } = null!;
    public string Address { get; set; } = null!;
    public VendorStatus Status { get; set; } = VendorStatus.Active;
    public string Notes { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}

using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Vendor;

public class VendorCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public VendorType VendorType { get; set; }
    public string GstNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public VendorStatus Status { get; set; } = VendorStatus.Active;
    public string Notes { get; set; } = string.Empty;
}

using System;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Equipment;
using CleanArchitecture.Shared.Models.Vendor;

namespace CleanArchitecture.Shared.Models.AmcContract;

public class AmcContractViewModel
{
    public Guid Id { get; set; }
    public string AmcCode { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractTitle { get; set; } = string.Empty;
    public AmcContractType ContractType { get; set; }
    public Guid EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public EquipmentViewModel? Equipment { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string VendorCompanyName { get; set; } = string.Empty;
    public VendorViewModel? Vendor { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal ContractAmount { get; set; }
    public AmcPaymentTerm PaymentTerms { get; set; }
    public AmcServiceFrequency ServiceFrequency { get; set; }
    public string CoverageDetails { get; set; } = string.Empty;
    public string Exclusions { get; set; } = string.Empty;
    public string DocumentLink { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public AmcStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
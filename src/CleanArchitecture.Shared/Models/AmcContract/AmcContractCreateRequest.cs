using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.AmcContract;

public class AmcContractCreateRequest
{
    public string? AmcCode { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractTitle { get; set; } = string.Empty;
    public AmcContractType ContractType { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid VendorId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal ContractAmount { get; set; }
    public AmcPaymentTerm PaymentTerms { get; set; }
    public AmcServiceFrequency ServiceFrequency { get; set; }
    public string CoverageDetails { get; set; } = string.Empty;
    public string Exclusions { get; set; } = string.Empty;
    public string DocumentLink { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public AmcStatus Status { get; set; } = AmcStatus.Active;
}

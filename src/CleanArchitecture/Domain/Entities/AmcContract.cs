using System;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Domain.Entities;

public class AmcContract
{
    public Guid Id { get; set; }
    public string AmcCode { get; set; } = null!;
    public string ContractNumber { get; set; } = null!;
    public string ContractTitle { get; set; } = null!;
    public AmcContractType ContractType { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid VendorId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal ContractAmount { get; set; }
    public AmcPaymentTerm PaymentTerms { get; set; }
    public AmcServiceFrequency ServiceFrequency { get; set; }
    public string CoverageDetails { get; set; } = null!;
    public string Exclusions { get; set; } = null!;
    public string DocumentLink { get; set; } = null!;
    public string Remarks { get; set; } = null!;
    public AmcStatus Status { get; set; } = AmcStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid? RestoredBy { get; set; }

    public virtual Equipment? Equipment { get; set; }
    public virtual Vendor? Vendor { get; set; }
}

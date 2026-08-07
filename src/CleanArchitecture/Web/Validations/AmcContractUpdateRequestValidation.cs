using CleanArchitecture.Shared.Models.AmcContract;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class AmcContractUpdateRequestValidation : AbstractValidator<AmcContractUpdateRequest>
{
    public AmcContractUpdateRequestValidation()
    {
        RuleFor(x => x.AmcCode)
            .NotEmpty().WithMessage("AMC code is required.")
            .MaximumLength(50).WithMessage("AMC code must not exceed 50 characters.");

        RuleFor(x => x.ContractNumber)
            .NotEmpty().WithMessage("Contract number is required.")
            .MaximumLength(100).WithMessage("Contract number must not exceed 100 characters.");

        RuleFor(x => x.ContractTitle)
            .NotEmpty().WithMessage("Contract title is required.")
            .MaximumLength(255).WithMessage("Contract title must not exceed 255 characters.");

        RuleFor(x => x.ContractType)
            .IsInEnum().WithMessage("Contract type is invalid. Valid values: Comprehensive, NonComprehensive, PreventativeMaintenance, BreakdownMaintenance, OperationsAndMaintenance, Other.");

        RuleFor(x => x.EquipmentId)
            .NotEmpty().WithMessage("Equipment ID is required.");

        RuleFor(x => x.VendorId)
            .NotEmpty().WithMessage("Vendor ID is required.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        RuleFor(x => x.ContractAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Contract amount must be greater than or equal to 0.");

        RuleFor(x => x.PaymentTerms)
            .IsInEnum().WithMessage("Payment terms is invalid. Valid values: BankTransfer, Cheque, Cash, UpiDigitalTransfer, QuarterlyAdvance, MonthlyPostpaid, Other.");

        RuleFor(x => x.ServiceFrequency)
            .IsInEnum().WithMessage("Service frequency is invalid. Valid values: Monthly, Quarterly, HalfYearly, Yearly, OneTime.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status is invalid. Valid values: Active, Expiring, Expired, Cancelled.");
    }
}

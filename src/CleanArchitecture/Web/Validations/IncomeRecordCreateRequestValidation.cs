using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Income;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class IncomeRecordCreateRequestValidation : AbstractValidator<IncomeRecordCreateRequest>
{
    public IncomeRecordCreateRequestValidation()
    {
        RuleFor(x => x.IncomeEntity)
            .NotNull().WithMessage("Income entity is required. Valid values: ApartmentWise, GeneralOthers.")
            .IsInEnum().WithMessage("Income entity is invalid. Valid values: ApartmentWise, GeneralOthers.");

        RuleFor(x => x.IncomeType)
            .NotNull().WithMessage("Income type is required. Valid values: Rent, Maintenance, SecurityDeposit, WaterCharge, Others.")
            .IsInEnum().WithMessage("Income type is invalid. Valid values: Rent, Maintenance, SecurityDeposit, WaterCharge, Others.");

        RuleFor(x => x.Amount)
            .NotNull().WithMessage("Amount is required.")
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.PaymentDate)
            .NotNull().WithMessage("Payment date is required.");

        RuleFor(x => x.PaymentMethod)
            .NotNull().WithMessage("Payment method is required. Valid values: TransferFromNestaway, DirectFromTenant, Cash, BankTransfer, Cheque, Others.")
            .IsInEnum().WithMessage("Payment method is invalid. Valid values: TransferFromNestaway, DirectFromTenant, Cash, BankTransfer, Cheque, Others.");

        RuleFor(x => x.Status)
            .NotNull().WithMessage("Status is required. Valid values: Paid, Pending, Overdue, Partial.")
            .IsInEnum().WithMessage("Status is invalid. Valid values: Paid, Pending, Overdue, Partial.");

        RuleFor(x => x.BuildingId)
            .NotNull().When(x => x.IncomeEntity == IncomeEntity.ApartmentWise)
            .WithMessage("Building is required for apartment-wise income. Please select a building.");

        RuleFor(x => x.ApartmentId)
            .NotNull().When(x => x.IncomeEntity == IncomeEntity.ApartmentWise)
            .WithMessage("Apartment is required for apartment-wise income. Please select an apartment.");
    }
}

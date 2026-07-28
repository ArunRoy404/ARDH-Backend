using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Income;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class IncomeRecordUpdateRequestValidation : AbstractValidator<IncomeRecordUpdateRequest>
{
    public IncomeRecordUpdateRequestValidation()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Income Record ID is required.");

        RuleFor(x => x.IncomeEntity)
            .IsInEnum().WithMessage("A valid income entity type is required.");

        RuleFor(x => x.IncomeType)
            .IsInEnum().WithMessage("A valid income type is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Period)
            .NotEmpty().WithMessage("Period is required.");

        RuleFor(x => x.PaymentDate)
            .NotEmpty().WithMessage("Payment date is required.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("A valid payment method is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid status is required.");

        RuleFor(x => x.BuildingId)
            .NotEmpty().When(x => x.IncomeEntity == IncomeEntity.ApartmentWise)
            .WithMessage("Building is required for apartment-wise income.");

        RuleFor(x => x.ApartmentId)
            .NotEmpty().When(x => x.IncomeEntity == IncomeEntity.ApartmentWise)
            .WithMessage("Apartment is required for apartment-wise income.");

        RuleFor(x => x.TenantId)
            .NotEmpty().When(x => x.IncomeEntity == IncomeEntity.ApartmentWise)
            .WithMessage("Tenant is required for apartment-wise income.");
    }
}

using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Expenses;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class ExpenseRecordUpdateRequestValidation : AbstractValidator<ExpenseRecordUpdateRequest>
{
    public ExpenseRecordUpdateRequestValidation()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Expense record ID is required.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("A valid expense category type is required.");

        RuleFor(x => x.ExpenseHead)
            .NotEmpty().When(x => x.Category == ExpenseCategory.Utility)
            .WithMessage("Expense head (sub category) is required when category is set to Utility.");

        RuleFor(x => x.SpecificItem)
            .NotEmpty().WithMessage("Specific item is required.");

        RuleFor(x => x.Nature)
            .IsInEnum().WithMessage("A valid expense nature is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Entity)
            .IsInEnum().WithMessage("A valid expense entity type is required.");

        RuleFor(x => x.BuildingId)
            .NotEmpty().When(x => x.Entity == ExpenseEntity.BuildingLevel || x.Entity == ExpenseEntity.ApartmentSpecific)
            .WithMessage("Building is required for Building Level or Apartment Specific entity.");

        RuleFor(x => x.ApartmentId)
            .NotEmpty().When(x => x.Entity == ExpenseEntity.ApartmentSpecific)
            .WithMessage("Apartment is required for Apartment Specific entity.");

        RuleFor(x => x.ExpenseDate)
            .NotEmpty().WithMessage("Expense date is required.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("A valid payment method is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid status is required.");

        // Water tank delivery fields conditional validation
        RuleFor(x => x.TankerNumber)
            .NotEmpty().When(x => IsWaterTankSubcategory(x.ExpenseHead))
            .WithMessage("Tanker number is required for water tank delivery.");

        RuleFor(x => x.TimeOfDelivery)
            .NotEmpty().When(x => IsWaterTankSubcategory(x.ExpenseHead))
            .WithMessage("Time of delivery is required for water tank delivery.");

        RuleFor(x => x.DeliveryDriverName)
            .NotEmpty().When(x => IsWaterTankSubcategory(x.ExpenseHead))
            .WithMessage("Delivery driver name is required for water tank delivery.");

        RuleFor(x => x.ManagerInAttendance)
            .NotEmpty().When(x => IsWaterTankSubcategory(x.ExpenseHead))
            .WithMessage("Manager in attendance is required for water tank delivery.");

        RuleFor(x => x.LitersFilled)
            .NotNull().When(x => IsWaterTankSubcategory(x.ExpenseHead))
            .WithMessage("Liters filled is required for water tank delivery.")
            .GreaterThan(0).When(x => IsWaterTankSubcategory(x.ExpenseHead))
            .WithMessage("Liters filled must be greater than 0.");
    }

    private bool IsWaterTankSubcategory(string? expenseHead)
    {
        if (string.IsNullOrWhiteSpace(expenseHead)) return false;
        var eh = expenseHead.ToLowerInvariant();
        return eh.Contains("water tank") || eh.Contains("supplemental water") || eh.Contains("sweet water");
    }
}

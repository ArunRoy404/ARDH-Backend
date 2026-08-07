using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Expenses;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class ExpenseRecordCreateRequestValidation : AbstractValidator<ExpenseRecordCreateRequest>
{
    public ExpenseRecordCreateRequestValidation()
    {
        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("A valid expense category is required. Valid values: Utility, Operational, Maintenance, Tax, Capital.");

        RuleFor(x => x.ExpenseHead)
            .NotNull().WithMessage("Expense head is required.")
            .NotEmpty().WithMessage("Expense head is required.");

        RuleFor(x => x.SpecificItem)
            .NotNull().WithMessage("Specific item is required.")
            .NotEmpty().WithMessage("Specific item is required.");

        RuleFor(x => x.Nature)
            .NotNull().WithMessage("Expense nature is required. Valid values: Service, Material, Others.")
            .IsInEnum().WithMessage("Expense nature is required. Valid values: Service, Material, Others.");

        RuleFor(x => x.Amount)
            .NotNull().WithMessage("Amount is required.")
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Entity)
            .NotNull().WithMessage("Expense entity is required. Valid values: General, ApartmentSpecific, BuildingLevel.")
            .IsInEnum().WithMessage("Expense entity is required. Valid values: General, ApartmentSpecific, BuildingLevel.");

        RuleFor(x => x.BuildingId)
            .NotNull().WithMessage("Please select a building.");

        RuleFor(x => x.ApartmentId)
            .NotEmpty().When(x => x.Entity == ExpenseEntity.ApartmentSpecific)
            .WithMessage("Apartment is required for Apartment Specific entity.");

        RuleFor(x => x.ExpenseDate)
            .NotNull().WithMessage("Expense date is required.")
            .Must(x => !x.HasValue || x.Value.Date <= DateTime.UtcNow.AddHours(3).Date)
            .WithMessage("Expense date cannot be in the future. Please use today's date or an earlier date.");

        RuleFor(x => x.PaymentMethod)
            .NotNull().WithMessage("Payment method is required. For example: Cash, BankTransfer, Cheque, UPI, Card.")
            .NotEmpty().WithMessage("Payment method is required. For example: Cash, BankTransfer, Cheque, UPI, Card.");

        RuleFor(x => x.Status)
            .NotNull().WithMessage("Status is required. Valid values: Draft, PendingPayment, Paid.")
            .IsInEnum().WithMessage("Status is required. Valid values: Draft, PendingPayment, Paid.");

        // Water tank delivery fields - all or nothing, required for water tank deliveries
        RuleFor(x => x.TankerNumber)
            .NotEmpty().When(x => RequiresTankerFields(x))
            .WithMessage("Tanker number is required for water tank deliveries.");

        RuleFor(x => x.TimeOfDelivery)
            .NotEmpty().When(x => RequiresTankerFields(x))
            .WithMessage("Delivery time is required for water tank deliveries.");

        RuleFor(x => x.DeliveryDriverName)
            .NotEmpty().When(x => RequiresTankerFields(x))
            .WithMessage("Delivery driver name is required for water tank deliveries.");

        RuleFor(x => x.ManagerInAttendance)
            .NotEmpty().When(x => RequiresTankerFields(x))
            .WithMessage("Manager in attendance is required for water tank deliveries.");

        RuleFor(x => x.LitersFilled)
            .NotNull().When(x => RequiresTankerFields(x))
            .WithMessage("Liters filled is required for water tank deliveries.")
            .GreaterThan(0).When(x => RequiresTankerFields(x))
            .WithMessage("Liters filled must be greater than 0.");
    }

    private static bool RequiresTankerFields(ExpenseRecordCreateRequest x)
    {
        if (IsWaterTankerItem(x.SpecificItem)) return true;
        return !string.IsNullOrWhiteSpace(x.TankerNumber)
            || x.TimeOfDelivery.HasValue
            || !string.IsNullOrWhiteSpace(x.DeliveryDriverName)
            || !string.IsNullOrWhiteSpace(x.ManagerInAttendance)
            || x.LitersFilled.HasValue;
    }

    private static bool IsWaterTankerItem(string? specificItem)
    {
        if (string.IsNullOrWhiteSpace(specificItem)) return false;
        return specificItem.Trim().ToLowerInvariant().Contains("water tanker");
    }
}

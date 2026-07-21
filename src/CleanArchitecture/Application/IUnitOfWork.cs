using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application;

public interface IUnitOfWork
{
    IUserRepository UserRepository { get; }
    IForgotPasswordRepository ForgotPasswordRepository { get; }
    IBuildingRepository BuildingRepository { get; }
    ISettingRepository SettingRepository { get; }
    IDeletedHistoryRepository DeletedHistoryRepository { get; }
    IOwnerRepository OwnerRepository { get; }
    IApartmentRepository ApartmentRepository { get; }
    ITenantRepository TenantRepository { get; }
    ITenantMoveOutRecordRepository TenantMoveOutRecordRepository { get; }
    IVendorRepository VendorRepository { get; }
    IEquipmentRepository EquipmentRepository { get; }
    IAmcContractRepository AmcContractRepository { get; }
    IMaintenanceRequestRepository MaintenanceRequestRepository { get; }
    IIncomeRecordRepository IncomeRecordRepository { get; }
    IExpenseRecordRepository ExpenseRecordRepository { get; }
    Task SaveChangesAsync(CancellationToken token);
    Task ExecuteTransactionAsync(Action action, CancellationToken token);
    Task ExecuteTransactionAsync(Func<Task> action, CancellationToken token);
}

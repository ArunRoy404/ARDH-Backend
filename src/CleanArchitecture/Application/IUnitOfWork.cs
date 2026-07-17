using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application;

public interface IUnitOfWork
{
    IUserRepository UserRepository { get; }
    IForgotPasswordRepository ForgotPasswordRepository { get; }
    IBuildingRepository BuildingRepository { get; }
    ISettingRepository SettingRepository { get; }
    Task SaveChangesAsync(CancellationToken token);
    Task ExecuteTransactionAsync(Action action, CancellationToken token);
    Task ExecuteTransactionAsync(Func<Task> action, CancellationToken token);
}

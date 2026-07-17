using CleanArchitecture.Application;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Repositories;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public IUserRepository UserRepository { get; }
    public IForgotPasswordRepository ForgotPasswordRepository { get; }
    public IBuildingRepository BuildingRepository { get; }

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _context = dbContext;
        UserRepository = new UserRepository(_context);
        ForgotPasswordRepository = new ForgotPasswordRepository(_context);
        BuildingRepository = new BuildingRepository(_context);
    }

    public async Task SaveChangesAsync(CancellationToken token)
        => await _context.SaveChangesAsync(token);

    public async Task ExecuteTransactionAsync(Action action, CancellationToken token)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(token);
        try
        {
            action();
            await _context.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(token);
            throw TransactionException.TransactionNotExecuteException(ex);
        }
    }

    public async Task ExecuteTransactionAsync(Func<Task> action, CancellationToken token)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(token);
        try
        {
            await action();
            await _context.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(token);
            throw TransactionException.TransactionNotExecuteException(ex);
        }
    }
}

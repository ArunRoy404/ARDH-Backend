using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class ExpenseRecordRepository(ApplicationDbContext dbContext) : GenericRepository<ExpenseRecord>(dbContext), IExpenseRecordRepository
{
}

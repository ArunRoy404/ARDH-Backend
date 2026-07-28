using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class IncomeRecordRepository(ApplicationDbContext dbContext) : GenericRepository<IncomeRecord>(dbContext), IIncomeRecordRepository
{
}

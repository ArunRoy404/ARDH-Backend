using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class TenantMoveOutRecordRepository(ApplicationDbContext dbContext) : GenericRepository<TenantMoveOutRecord>(dbContext), ITenantMoveOutRecordRepository
{
}

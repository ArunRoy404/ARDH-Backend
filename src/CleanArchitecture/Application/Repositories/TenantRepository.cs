using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class TenantRepository(ApplicationDbContext dbContext) : GenericRepository<Tenant>(dbContext), ITenantRepository
{
}

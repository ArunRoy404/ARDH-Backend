using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class AmcContractRepository(ApplicationDbContext dbContext) : GenericRepository<AmcContract>(dbContext), IAmcContractRepository
{
}

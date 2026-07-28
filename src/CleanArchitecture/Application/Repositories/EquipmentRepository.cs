using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class EquipmentRepository(ApplicationDbContext dbContext) : GenericRepository<Equipment>(dbContext), IEquipmentRepository
{
}

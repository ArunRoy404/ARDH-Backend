using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class ApartmentRepository(ApplicationDbContext dbContext) : GenericRepository<Apartment>(dbContext), IApartmentRepository
{
}

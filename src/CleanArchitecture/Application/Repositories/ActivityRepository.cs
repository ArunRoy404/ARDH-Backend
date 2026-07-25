using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class ActivityRepository(ApplicationDbContext context) : GenericRepository<Activity>(context), IActivityRepository
{
}

using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class TenantRentHistoryRepository(ApplicationDbContext context) : GenericRepository<TenantRentHistory>(context), ITenantRentHistoryRepository { }

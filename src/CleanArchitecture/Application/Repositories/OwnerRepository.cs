using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class OwnerRepository(ApplicationDbContext dbContext) : GenericRepository<Owner>(dbContext), IOwnerRepository { }

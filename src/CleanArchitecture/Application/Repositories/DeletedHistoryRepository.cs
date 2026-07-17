using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class DeletedHistoryRepository(ApplicationDbContext context) : GenericRepository<DeletedHistory>(context), IDeletedHistoryRepository { }

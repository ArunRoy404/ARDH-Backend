using CleanArchitecture.Application.Repositories;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class BulkUploadRepository(ApplicationDbContext dbContext) : GenericRepository<BulkUpload>(dbContext), IBulkUploadRepository
{
}

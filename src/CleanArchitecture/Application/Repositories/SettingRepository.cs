using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class SettingRepository(ApplicationDbContext context) : GenericRepository<Setting>(context), ISettingRepository { }

using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class EmailReminderLogRepository(ApplicationDbContext context) : GenericRepository<EmailReminderLog>(context), IEmailReminderLogRepository { }

using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Interface;

namespace CleanArchitecture.Application.Repositories;

public class NotificationRecipientRepository(ApplicationDbContext context) : GenericRepository<NotificationRecipient>(context), INotificationRecipientRepository { }

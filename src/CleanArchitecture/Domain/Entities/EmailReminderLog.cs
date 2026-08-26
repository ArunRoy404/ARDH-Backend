using System;

namespace CleanArchitecture.Domain.Entities;

public class EmailReminderLog
{
    public Guid Id { get; set; }
    public string ReminderType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public Guid UserId { get; set; }
    public DateTime SentAt { get; set; }
}

using System;

namespace CleanArchitecture.Domain.Entities;

public class NotificationRecipient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Navigation properties for EF Core relationships
    public virtual Notification? Notification { get; set; }
    public virtual User? User { get; set; }
}

namespace CleanArchitecture.Shared.Models.Notification;

public class NotificationCreateRequest
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

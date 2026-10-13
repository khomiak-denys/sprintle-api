using System.Text.Json.Serialization;

namespace Notifications.Domain.Notifications
{
    /// <summary>
    /// Specifies the delivery channel or medium for a notification.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Delivery via email message.
        /// </summary>
        Email = 0
    }
}

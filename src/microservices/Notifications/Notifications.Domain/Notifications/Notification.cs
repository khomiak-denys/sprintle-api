using DomainFramework;

namespace Notifications.Domain.Notifications
{
    /// <summary>
    /// Represents a notification entity configuring user notification settings and channel preferences.
    /// Inherits auditing and identity fields (Id, CreatedAt, UpdatedAt) from <see cref="EntityBase"/>.
    /// </summary>
    public class Notification : EntityBase
    {
        /// <summary>Gets the unique identifier of the user associated with this notification.</summary>
        public Guid UserId { get; private set; }

        /// <summary>Gets the email address designated for receiving notifications.</summary>
        public string Email { get; private set; } = null!;

        /// <summary>Gets the username associated with this notification.</summary>
        public string Username { get; private set; } = null!;

        /// <summary>Gets the notification channel type.</summary>
        public NotificationType NotificationType { get; private set; }

        /// <summary>Gets a value indicating whether notifications are active and enabled.</summary>
        public bool Enabled { get; private set; }

        /// <summary>EF Core parameterless constructor.</summary>
        private Notification() { }

        private Notification(
            Guid userId,
            string email,
            string username,
            NotificationType notificationType,
            bool enabled)
        {
            UserId = userId;
            Email = email;
            Username = username;
            NotificationType = notificationType;
            Enabled = enabled;
        }

        private Notification(
            Guid id,
            Guid userId,
            string email,
            string username,
            NotificationType notificationType,
            bool enabled) : base(id)
        {
            UserId = userId;
            Email = email;
            Username = username;
            NotificationType = notificationType;
            Enabled = enabled;
        }

        /// <summary>
        /// Factory method that creates a new <see cref="Notification"/> instance with a generated identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="email">The user's email address.</param>
        /// <param name="username">The user's display name or username.</param>
        /// <param name="notificationType">The channel type for notifications (defaults to Email).</param>
        /// <param name="enabled">Indicates whether notifications are initially enabled (defaults to true).</param>
        /// <returns>A fully initialized <see cref="Notification"/> entity.</returns>
        public static Notification Create(
            Guid userId,
            string email,
            string username,
            NotificationType notificationType = NotificationType.Email,
            bool enabled = true)
        {
            var notification = new Notification(userId, email, username, notificationType, enabled);
            notification.OnCreate();
            return notification;
        }

        /// <summary>
        /// Factory method that creates a new <see cref="Notification"/> instance with a specified identifier.
        /// </summary>
        /// <param name="id">The explicit identifier for the notification entity.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="email">The user's email address.</param>
        /// <param name="username">The user's display name or username.</param>
        /// <param name="notificationType">The channel type for notifications.</param>
        /// <param name="enabled">Indicates whether notifications are initially enabled.</param>
        /// <returns>A fully initialized <see cref="Notification"/> entity with the specified identifier.</returns>
        public static Notification Create(
            Guid id,
            Guid userId,
            string email,
            string username,
            NotificationType notificationType = NotificationType.Email,
            bool enabled = true)
        {
            var notification = new Notification(id, userId, email, username, notificationType, enabled);
            notification.OnCreate();
            return notification;
        }

        /// <summary>
        /// Updates the email address associated with this notification.
        /// </summary>
        /// <param name="email">The updated email address.</param>
        public void UpdateEmail(string email)
        {
            Email = email;
            OnModify();
        }

        /// <summary>
        /// Updates the username associated with this notification.
        /// </summary>
        /// <param name="username">The updated username.</param>
        public void UpdateUsername(string username)
        {
            Username = username;
            OnModify();
        }

        /// <summary>
        /// Updates mutable contact details and enablement status.
        /// </summary>
        /// <param name="email">The updated email address.</param>
        /// <param name="username">The updated username.</param>
        /// <param name="enabled">The updated enablement status.</param>
        public void Update(string email, string username, bool enabled)
        {
            Email = email;
            Username = username;
            Enabled = enabled;
            OnModify();
        }

        /// <summary>
        /// Sets whether this notification is enabled or disabled.
        /// </summary>
        /// <param name="enabled"><c>true</c> to enable notifications; otherwise <c>false</c>.</param>
        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            OnModify();
        }

        /// <summary>
        /// Enables notifications for this channel.
        /// </summary>
        public void Enable()
        {
            SetEnabled(true);
        }

        /// <summary>
        /// Disables notifications for this channel.
        /// </summary>
        public void Disable()
        {
            SetEnabled(false);
        }
    }
}

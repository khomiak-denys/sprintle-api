using System.Text.RegularExpressions;
using Messaging.Abstractions;

namespace MessageBus.Convention
{
    /// <summary>
    /// Resolves RabbitMQ routing keys from event types using lowercase dot-separated notation.
    /// </summary>
    public sealed partial class RabbitMqRoutingConvention
        : IEventRoutingConvention
    {
        /// <inheritdoc />
        public string GetRoutingKey(Type eventType)
        {
            ArgumentNullException.ThrowIfNull(eventType);

            var name = eventType.Name;

            // TaskAssigned -> task_assigned
            var snakeCase = SnakeCaseRegex().Replace(
                name,
                "$1_$2");

            return snakeCase.ToLowerInvariant()
                .Replace('_', '.');
        }

        [GeneratedRegex("([a-z0-9])([A-Z])")]
        private static partial Regex SnakeCaseRegex();
    }
}

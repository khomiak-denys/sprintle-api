using System;
using System.Collections.Generic;
using System.Text.Json;
using DomainFramework;
using Workspaces.Domain.Tasks.Events;

namespace Workspaces.Infrastructure.Persistence.EventSourcing
{
    /// <summary>
    /// Serializes and deserializes task domain events to and from JSON format.
    /// </summary>
    public static class TaskEventSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly Dictionary<string, Type> TypeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(TaskCreatedDomainEvent)] = typeof(TaskCreatedDomainEvent),
            [nameof(TaskAssignedDomainEvent)] = typeof(TaskAssignedDomainEvent),
            [nameof(TaskSubmittedForReviewDomainEvent)] = typeof(TaskSubmittedForReviewDomainEvent),
            [nameof(TaskApprovedDomainEvent)] = typeof(TaskApprovedDomainEvent),
            [nameof(TaskClosedDomainEvent)] = typeof(TaskClosedDomainEvent),
            [nameof(TaskCancelledDomainEvent)] = typeof(TaskCancelledDomainEvent)
        };

        /// <summary>
        /// Serializes a domain event into its type name and JSON payload.
        /// </summary>
        /// <param name="event">The domain event to serialize.</param>
        /// <returns>A tuple of event type name and JSON payload string.</returns>
        public static (string EventType, string Payload) Serialize(IDomainEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event);

            var eventType = @event.GetType().Name;
            var payload = JsonSerializer.Serialize(@event, @event.GetType(), JsonOptions);
            return (eventType, payload);
        }

        /// <summary>
        /// Deserializes a domain event from its type name and JSON payload.
        /// </summary>
        /// <param name="eventType">The type name of the event.</param>
        /// <param name="payload">The JSON payload string.</param>
        /// <returns>The deserialized <see cref="IDomainEvent"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when event type is unknown or deserialization fails.</exception>
        public static IDomainEvent Deserialize(string eventType, string payload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
            ArgumentException.ThrowIfNullOrWhiteSpace(payload);

            if (!TypeMap.TryGetValue(eventType, out var targetType))
            {
                throw new InvalidOperationException($"Unknown event type '{eventType}' cannot be deserialized.");
            }

            var result = JsonSerializer.Deserialize(payload, targetType, JsonOptions);
            return result as IDomainEvent
                ?? throw new InvalidOperationException($"Failed to deserialize payload to '{eventType}'.");
        }
    }
}

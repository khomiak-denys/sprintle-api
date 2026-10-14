using System;

namespace Workspaces.Infrastructure.Persistence.Entities
{
    /// <summary>
    /// Represents an immutable serialized domain event stored in the event store table.
    /// </summary>
    public class TaskEventRecord
    {
        /// <summary>
        /// Gets or sets the unique event identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the unique task aggregate identifier.
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// Gets or sets the sequence number / version of this event within the task event stream.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the type name of the serialized event.
        /// </summary>
        public string EventType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the JSON payload of the event.
        /// </summary>
        public string Payload { get; set; } = null!;

        /// <summary>
        /// Gets or sets the UTC timestamp when the domain event occurred.
        /// </summary>
        public DateTime OccurredAt { get; set; }
    }
}

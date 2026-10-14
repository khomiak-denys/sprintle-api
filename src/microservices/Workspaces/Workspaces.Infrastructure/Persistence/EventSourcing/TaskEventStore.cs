using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainFramework;
using Microsoft.EntityFrameworkCore;
using Workspaces.Domain.Tasks;
using Workspaces.Infrastructure.Persistence.Entities;

namespace Workspaces.Infrastructure.Persistence.EventSourcing
{
    /// <summary>
    /// EF Core implementation of <see cref="ITaskEventStore"/> for persisting and rehydrating task event streams.
    /// </summary>
    public sealed class TaskEventStore : ITaskEventStore
    {
        private readonly WorkspacesDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskEventStore"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public TaskEventStore(WorkspacesDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var records = await _context.TaskEvents
                .Where(e => e.TaskId == taskId)
                .OrderBy(e => e.Version)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (records.Count == 0)
            {
                return Array.Empty<IDomainEvent>();
            }

            var events = new List<IDomainEvent>(records.Count);
            foreach (var record in records)
            {
                events.Add(TaskEventSerializer.Deserialize(record.EventType, record.Payload));
            }

            return events;
        }

        /// <inheritdoc/>
        public async Task AppendEventsAsync(
            Guid taskId,
            int expectedVersion,
            IReadOnlyCollection<IDomainEvent> events,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(events);

            if (events.Count == 0)
            {
                return;
            }

            var currentMaxVersion = await _context.TaskEvents
                .Where(e => e.TaskId == taskId)
                .MaxAsync(e => (int?)e.Version, cancellationToken) ?? 0;

            if (currentMaxVersion != expectedVersion)
            {
                throw new InvalidOperationException(
                    $"Optimistic concurrency conflict on task stream '{taskId}'. Expected version {expectedVersion}, found {currentMaxVersion}.");
            }

            var nextVersion = expectedVersion;
            foreach (var @event in events)
            {
                nextVersion++;
                var (eventType, payload) = TaskEventSerializer.Serialize(@event);

                _context.TaskEvents.Add(new TaskEventRecord
                {
                    Id = @event.EventId != Guid.Empty ? @event.EventId : Guid.NewGuid(),
                    TaskId = taskId,
                    Version = nextVersion,
                    EventType = eventType,
                    Payload = payload,
                    OccurredAt = @event.OccurredAt
                });
            }
        }
    }
}

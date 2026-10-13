namespace Notifications.Application.Abstractions
{
    /// <summary>
    /// Abstracts the database transaction boundary.
    /// Implemented by the EF Core DbContext in <c>Notifications.Infrastructure</c>.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Persists all pending changes in the persistence context to the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of state entries written to the database.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

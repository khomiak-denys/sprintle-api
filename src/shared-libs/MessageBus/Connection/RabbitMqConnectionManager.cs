using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace MessageBus.Connection
{
    /// <summary>
    /// Manages the lifecycle of the RabbitMQ connection.
    /// </summary>
    /// <param name="configuration">The configuration containing connection strings.</param>
    /// <param name="logger">The logger instance.</param>
    public sealed partial class RabbitMqConnectionManager(
        IConfiguration configuration,
        ILogger<RabbitMqConnectionManager> logger)
        : IHostedService, IAsyncDisposable
    {
        private IConnection? _connection;

        /// <summary>
        /// Gets the active RabbitMQ connection.
        /// </summary>
        public IConnection Connection =>
            _connection
            ?? throw new InvalidOperationException(
                "Message bus connection is not initialized.");

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(
                    configuration.GetConnectionString("message-bus")
                    ?? throw new InvalidOperationException(
                        "Message bus connection string is not configured.")),

                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                RequestedHeartbeat = TimeSpan.FromSeconds(30)
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);

            _connection.ConnectionShutdownAsync += (_, args) =>
            {
                LogConnectionShutdown(args.ReplyText);

                return Task.CompletedTask;
            };

            _connection.RecoverySucceededAsync += (_, _) =>
            {
                LogConnectionRecovered();

                return Task.CompletedTask;
            };

            _connection.ConnectionRecoveryErrorAsync += (_, args) =>
            {
                LogConnectionRecoveryError(args.Exception);

                return Task.CompletedTask;
            };

            LogConnectionEstablished();
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
            => DisposeAsync().AsTask();

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            var connection = Interlocked.Exchange(
                ref _connection, null);

            if (connection is not null)
            {
                await connection.DisposeAsync();
                LogConnectionDisposed();
            }
        }

        [LoggerMessage(Level = LogLevel.Warning, Message = "Message bus connection shutdown: {Reason}")]
        private partial void LogConnectionShutdown(string? reason);

        [LoggerMessage(Level = LogLevel.Information, Message = "Message bus connection recovered.")]
        private partial void LogConnectionRecovered();

        [LoggerMessage(Level = LogLevel.Error, Message = "Message bus connection recovery failed.")]
        private partial void LogConnectionRecoveryError(Exception? exception);

        [LoggerMessage(Level = LogLevel.Information, Message = "Message bus connection established.")]
        private partial void LogConnectionEstablished();

        [LoggerMessage(Level = LogLevel.Information, Message = "Message bus connection disposed.")]
        private partial void LogConnectionDisposed();
    }
}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace MessageBus.Connection
{

    public sealed class RabbitMqConnectionManager(
        IConfiguration configuration,
        ILogger<RabbitMqConnectionManager> logger)
        : IHostedService, IAsyncDisposable
    {
        private IConnection? _connection;

        public IConnection Connection =>
            _connection
            ?? throw new InvalidOperationException(
                "RabbitMQ connection is not initialized.");

        public async Task StartAsync(CancellationToken ct)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(
                    configuration["RabbitMQ:Uri"]
                    ?? throw new InvalidOperationException(
                        "RabbitMQ:Uri is not configured.")),

                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                RequestedHeartbeat = TimeSpan.FromSeconds(30)
            };

            _connection = await factory.CreateConnectionAsync(ct);

            _connection.ConnectionShutdownAsync += (_, args) =>
            {
                logger.LogWarning(
                    "RabbitMQ connection shutdown: {Reason}",
                    args.ReplyText);

                return Task.CompletedTask;
            };

            _connection.RecoverySucceededAsync += (_, _) =>
            {
                logger.LogInformation(
                    "RabbitMQ connection recovered.");

                return Task.CompletedTask;
            };

            _connection.ConnectionRecoveryErrorAsync += (_, args) =>
            {
                logger.LogError(
                    "RabbitMQ connection recovery failed: {Error}",
                    args.Exception);

                return Task.CompletedTask;
            };

            logger.LogInformation("RabbitMQ connection established.");
        }

        public Task StopAsync(CancellationToken ct)
            => DisposeAsync().AsTask();

        public async ValueTask DisposeAsync()
        {
            var connection = Interlocked.Exchange(
                ref _connection, null);

            if (connection is not null)
            {
                await connection.DisposeAsync();
                logger.LogInformation("RabbitMQ connection disposed.");
            }
        }
    }
}

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceDefaults.Authorization.IntegrationApiKey;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;

namespace Workspaces.Infrastructure.Clients
{
    /// <summary>
    /// HTTP client implementation for communicating with the Users microservice.
    /// </summary>
    public partial class UsersApiClient : IUsersApiClient
    {
        private const string ApiKeyHeader = "X-API-Key";
        private readonly HttpClient _httpClient;
        private readonly IOptions<IntegrationApiKeyOptions> _apiKeyOptions;
        private readonly ILogger<UsersApiClient> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersApiClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="apiKeyOptions">Options containing the internal API key.</param>
        /// <param name="logger">The logger instance.</param>
        public UsersApiClient(
            HttpClient httpClient,
            IOptions<IntegrationApiKeyOptions> apiKeyOptions,
            ILogger<UsersApiClient> logger)
        {
            _httpClient = httpClient;
            _apiKeyOptions = apiKeyOptions;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<UserResult?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/private-integrations/users/{userId}/contact-info");

            if (!string.IsNullOrWhiteSpace(_apiKeyOptions.Value.ApiKey))
            {
                request.Headers.Add(ApiKeyHeader, _apiKeyOptions.Value.ApiKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogGetContactInfoFailed(userId, (int)response.StatusCode);
                return null;
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<UserResult>(cancellationToken);
            }
            catch (Exception ex)
            {
                LogDeserializeContactInfoFailed(ex, userId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<Guid, UserResult>> GetBatchAsync(
            IReadOnlyCollection<Guid> userIds,
            int page = 1,
            int pageSize = 100,
            string? search = null,
            bool? isWorkspaceMember = null,
            CancellationToken cancellationToken = default)
        {
            if (isWorkspaceMember != false && (userIds == null || userIds.Count == 0))
            {
                return new Dictionary<Guid, UserResult>();
            }

            var distinctIds = userIds != null && userIds.Count > 0
                ? userIds.Distinct().ToList()
                : new List<Guid>();

            var effectivePage = Math.Max(1, page);
            var effectivePageSize = Math.Clamp(pageSize, 1, 100);

            var endpoint = $"api/v1/private-integrations/users/batch?page={effectivePage}&pageSize={effectivePageSize}";
            if (!string.IsNullOrWhiteSpace(search))
            {
                endpoint += $"&search={Uri.EscapeDataString(search.Trim())}";
            }

            if (isWorkspaceMember.HasValue)
            {
                endpoint += $"&isWorkspaceMember={isWorkspaceMember.Value.ToString().ToLowerInvariant()}";
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                endpoint)
            {
                Content = JsonContent.Create(new UsersBatchRequest(distinctIds))
            };

            if (!string.IsNullOrWhiteSpace(_apiKeyOptions.Value.ApiKey))
            {
                request.Headers.Add(ApiKeyHeader, _apiKeyOptions.Value.ApiKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogGetBatchFailed((int)response.StatusCode);
                return new Dictionary<Guid, UserResult>();
            }

            try
            {
                var pagedUsers = await response.Content.ReadFromJsonAsync<UsersBatchResponse>(cancellationToken);
                var resultDictionary = new Dictionary<Guid, UserResult>();
                if (pagedUsers?.Items is not null)
                {
                    foreach (var user in pagedUsers.Items)
                    {
                        resultDictionary[user.Id] = user;
                    }
                }

                return resultDictionary;
            }
            catch (Exception ex)
            {
                LogDeserializeBatchFailed(ex);
                return new Dictionary<Guid, UserResult>();
            }
        }

        [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to retrieve contact info for user '{UserId}' from Users API. StatusCode: {StatusCode}")]
        private partial void LogGetContactInfoFailed(Guid userId, int statusCode);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to deserialize contact info for user '{UserId}' from Users API.")]
        private partial void LogDeserializeContactInfoFailed(Exception ex, Guid userId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to retrieve batch users from Users API. StatusCode: {StatusCode}")]
        private partial void LogGetBatchFailed(int statusCode);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to deserialize batch users response from Users API.")]
        private partial void LogDeserializeBatchFailed(Exception ex);

        private sealed record UsersBatchRequest(IReadOnlyList<Guid> UserIds);

        private sealed record UsersBatchResponse(
            IReadOnlyList<UserResult>? Items,
            int Page = 1,
            int PageSize = 10,
            int TotalCount = 0);
    }
}

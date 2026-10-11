using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using ServiceDefaults.Authorization.IntegrationApiKey;
using Workspaces.Application.Abstractions;
using Workspaces.Application.Tasks.DTOs;

namespace Workspaces.Infrastructure.Clients
{
    /// <summary>
    /// HTTP client implementation for communicating with the Users microservice.
    /// </summary>
    public class UsersApiClient : IUsersApiClient
    {
        private const string ApiKeyHeader = "X-API-Key";
        private readonly HttpClient _httpClient;
        private readonly IOptions<IntegrationApiKeyOptions> _apiKeyOptions;

        public UsersApiClient(
            HttpClient httpClient,
            IOptions<IntegrationApiKeyOptions> apiKeyOptions)
        {
            _httpClient = httpClient;
            _apiKeyOptions = apiKeyOptions;
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
                return null;
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<UserResult>(cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<Guid, UserResult>> GetBatchAsync(
            IReadOnlyCollection<Guid> userIds,
            int page = 1,
            int pageSize = 100,
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            if (userIds == null || userIds.Count == 0)
            {
                return new Dictionary<Guid, UserResult>();
            }

            var distinctIds = userIds.Distinct().ToList();
            var effectivePage = Math.Max(1, page);
            var effectivePageSize = Math.Clamp(pageSize, 1, 100);

            var endpoint = $"api/v1/private-integrations/users/batch?page={effectivePage}&pageSize={effectivePageSize}";
            if (!string.IsNullOrWhiteSpace(search))
            {
                endpoint += $"&search={Uri.EscapeDataString(search.Trim())}";
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
            catch
            {
                return new Dictionary<Guid, UserResult>();
            }
        }

        private sealed record UsersBatchRequest(IReadOnlyList<Guid> UserIds);

        private sealed record UsersBatchResponse(IReadOnlyList<UserResult>? Items);
    }
}

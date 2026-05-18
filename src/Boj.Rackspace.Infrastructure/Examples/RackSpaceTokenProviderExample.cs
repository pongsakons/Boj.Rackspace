using Boj.RackSpace.Application.Interfaces.Authentication;
using System.Net;

namespace Boj.Rackspace.Infrastructure.Examples
{
    /// <summary>
    /// Complete example of using RackSpaceTokenProvider in a clean architecture application.
    /// 
    /// FEATURES:
    /// - Thread-safe implementation with SemaphoreSlim double-check locking pattern
    /// - IMemoryCache for token caching with configurable expiration
    /// - Polly retry policy with exponential backoff (3 retries by default)
    /// - Automatic token refresh on cache expiration
    /// - Comprehensive logging and error handling
    /// - CancellationToken support throughout
    /// - SOLID principles: Single Responsibility, Dependency Injection, Open/Closed
    /// 
    /// ARCHITECTURE:
    /// 1. Interface (IRackSpaceTokenProvider) - Abstraction in Application layer
    /// 2. Implementation (RackSpaceTokenProvider) - Service in Infrastructure layer
    /// 3. Options (RackSpaceTokenOptions) - Configuration in Infrastructure layer
    /// 4. Policies (RackSpaceResiliencePolicies) - Resilience patterns in Infrastructure layer
    /// 5. DI Registration - Configured in Program.cs
    /// </summary>
    public class RackSpaceTokenProviderExample
    {
        /// <summary>
        /// Example 1: Basic usage in a service
        /// </summary>
        public class ObjectStorageService
        {
            private readonly HttpClient _httpClient;
            private readonly IRackSpaceTokenProvider _tokenProvider;

            public ObjectStorageService(
                HttpClient httpClient,
                IRackSpaceTokenProvider tokenProvider)
            {
                _httpClient = httpClient;
                _tokenProvider = tokenProvider;
            }

            public async Task<string> DownloadFileAsync(
                string container,
                string objectName,
                CancellationToken cancellationToken)
            {
                // Get token (from cache or authenticate)
                var token = await _tokenProvider.GetTokenAsync(cancellationToken);

                // Use token in request
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://storage.api.rackspacecloud.com/v1/{container}/{objectName}");
                request.Headers.Add("X-Auth-Token", token);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }

            public async Task<string> UploadFileAsync(
                string container,
                string objectName,
                Stream fileStream,
                CancellationToken cancellationToken)
            {
                var token = await _tokenProvider.GetTokenAsync(cancellationToken);

                var request = new HttpRequestMessage(
                    HttpMethod.Put,
                    $"https://storage.api.rackspacecloud.com/v1/{container}/{objectName}");
                request.Headers.Add("X-Auth-Token", token);
                request.Content = new StreamContent(fileStream);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Example 2: Using with HttpMessageHandler for automatic token injection
        /// </summary>
        public class AuthTokenHandler : DelegatingHandler
        {
            private readonly IRackSpaceTokenProvider _tokenProvider;

            public AuthTokenHandler(IRackSpaceTokenProvider tokenProvider)
            {
                _tokenProvider = tokenProvider;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var token = await _tokenProvider.GetTokenAsync(cancellationToken);
                request.Headers.Add("X-Auth-Token", token);

                return await base.SendAsync(request, cancellationToken);
            }
        }

        /// <summary>
        /// Example 3: Handling token expiration with cache invalidation
        /// </summary>
        public class ResilientObjectClient
        {
            private readonly HttpClient _httpClient;
            private readonly IRackSpaceTokenProvider _tokenProvider;
            private const int MaxRetries = 2;

            public ResilientObjectClient(
                HttpClient httpClient,
                IRackSpaceTokenProvider tokenProvider)
            {
                _httpClient = httpClient;
                _tokenProvider = tokenProvider;
            }

            public async Task<HttpResponseMessage> SendRequestWithFallbackAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                for (int attempt = 0; attempt < MaxRetries; attempt++)
                {
                    var token = await _tokenProvider.GetTokenAsync(cancellationToken);
                    request.Headers.Remove("X-Auth-Token");
                    request.Headers.Add("X-Auth-Token", token);

                    var response = await _httpClient.SendAsync(request, cancellationToken);

                    // If unauthorized, invalidate cache and retry
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized 
                        && attempt < MaxRetries - 1)
                    {
                        _tokenProvider.InvalidateCache();
                        continue;
                    }

                    return response;
                }

                throw new InvalidOperationException("Failed to send request after all retries");
            }
        }

        /// <summary>
        /// Configuration in appsettings.json:
        /// 
        /// {
        ///   "RackSpace": {
        ///     "AuthEndpoint": "https://identity.api.rackspacecloud.com/v2.0/tokens",
        ///     "Username": "your-username",
        ///     "ApiKey": "your-api-key",
        ///     "CacheDurationMinutes": 1440,
        ///     "MaxRetryAttempts": 3,
        ///     "InitialRetryDelayMilliseconds": 500
        ///   }
        /// }
        /// 
        /// DI Registration in Program.cs:
        /// 
        /// // Configure options
        /// builder.Services.Configure<RackSpaceTokenOptions>(
        ///     builder.Configuration.GetSection(RackSpaceTokenOptions.SectionName));
        /// 
        /// // Add memory cache
        /// builder.Services.AddMemoryCache();
        /// 
        /// // Register token provider
        /// builder.Services.AddHttpClient<IRackSpaceTokenProvider, RackSpaceTokenProvider>();
        /// 
        /// // Register clients with auth handler
        /// builder.Services.AddHttpClient<IRackSpaceObjectClient, RackSpaceObjectClient>()
        ///     .AddHttpMessageHandler<AuthTokenHandler>();
        /// </summary>
        public static void ConfigurationExample()
        {
            // See comments above for the actual configuration
        }

        /// <summary>
        /// THREAD SAFETY DETAILS:
        /// 
        /// 1. SemaphoreSlim-based double-check locking:
        ///    - First check without lock (fast path for cached tokens)
        ///    - Acquire lock if cache miss
        ///    - Double-check after lock acquisition to prevent race conditions
        ///    - Release lock after token refresh
        /// 
        /// 2. IMemoryCache is thread-safe for read/write operations
        /// 
        /// 3. All async operations use ConfigureAwait(false) for better performance
        /// 
        /// RETRY POLICY DETAILS:
        /// 
        /// 1. Retries on:
        ///    - HttpRequestException (network errors)
        ///    - OperationCanceledException
        ///    - HTTP 408 (Request Timeout)
        ///    - HTTP 503 (Service Unavailable)
        ///    - HTTP 500 (Internal Server Error)
        ///    - HTTP 429 (Too Many Requests)
        /// 
        /// 2. Exponential backoff: 500ms * 2^(attempt-1)
        ///    - Attempt 1: 500ms
        ///    - Attempt 2: 1000ms
        ///    - Attempt 3: 2000ms
        /// 
        /// 3. Includes circuit breaker (optional): Prevents cascading failures
        /// 
        /// CACHING DETAILS:
        /// 
        /// 1. Absolute expiration: 24 hours (configurable)
        /// 2. Sliding expiration: 5 minutes (auto-refresh if not used)
        /// 3. Cache can be manually invalidated with InvalidateCache()
        /// 4. Thread-safe with SemaphoreSlim coordination
        /// </summary>
        public static void ThreadSafetyAndRetryDetails()
        {
            // See comments above for implementation details
        }
    }
}

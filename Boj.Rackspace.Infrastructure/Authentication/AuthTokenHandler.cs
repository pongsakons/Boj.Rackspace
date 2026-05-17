using Boj.RackSpace.Application.Interfaces.Authentication;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Boj.Rackspace.Infrastructure.Models;
using Boj.RackSpace.Application.Interfaces.Authentication;

namespace Boj.Rackspace.Infrastructure.Authentication
{
    /// <summary>
    /// HTTP message handler that automatically injects RackSpace authentication tokens
    /// into outgoing requests.
    /// 
    /// IMPORTANT: This handler reads tokens directly from IMemoryCache WITHOUT locking.
    /// It does NOT call GetTokenAsync() to avoid lock contention in the hot path.
    /// 
    /// Flow:
    /// 1. Try to get valid token from cache (no lock, ~0.1ms)
    /// 2. If token missing or expiring - call GetTokenAsync() to refresh
    /// 3. GetTokenAsync() handles all locking and refresh logic
    /// 4. Return with token in X-Auth-Token header
    /// 
    /// This separation ensures:
    /// - Low latency for typical requests (cache hits)
    /// - Proper concurrency control during refresh
    /// - Single refresh when multiple requests need it
    /// </summary>
    public class AuthTokenHandler : DelegatingHandler
    {
        private const string CacheKeyPrefix = "rackspace_token";
        
        private readonly IRackSpaceTokenProvider _tokenProvider;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AuthTokenHandler> _logger;

        public AuthTokenHandler(
            IRackSpaceTokenProvider tokenProvider,
            IMemoryCache cache,
            ILogger<AuthTokenHandler> logger)
        {
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // FAST PATH: Try to get token from cache without lock
            var token = GetTokenFromCacheIfValid();
            
            // SLOW PATH: If cache miss or token expiring, refresh
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("Token not found in cache, calling GetTokenAsync()");
                token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            }

            // Add token to request headers
            request.Headers.Add("X-Auth-Token", token);

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Tries to get a valid token directly from cache without locking.
        /// Returns null if token doesn't exist, is expired, or expiring soon.
        /// This is the hot path - executes without lock for ~99% of requests.
        /// </summary>
        private string? GetTokenFromCacheIfValid()
        {
            // Check if token exists in cache
            if (!_cache.TryGetValue(CacheKeyPrefix, out CachedTokenInfo? tokenInfo))
            {
                _logger.LogDebug("Token not found in cache");
                return null;
            }

            // Check if token is still valid and not expiring soon
            if (tokenInfo == null || !tokenInfo.IsValid || tokenInfo.ShouldRefresh)
            {
                _logger.LogDebug(
                    "Token needs refresh. IsValid: {IsValid}, ShouldRefresh: {ShouldRefresh}, Status: {Status}",
                    tokenInfo?.IsValid,
                    tokenInfo?.ShouldRefresh,
                    tokenInfo?.GetStatusSummary());
                return null;
            }

            _logger.LogDebug(
                "Token retrieved from cache. {Status}",
                tokenInfo.GetStatusSummary());

            return tokenInfo.Token;
        }
    }
}

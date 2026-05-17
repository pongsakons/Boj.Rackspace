namespace Boj.RackSpace.Application.Interfaces.Authentication
{
    /// <summary>
    /// Provides RackSpace authentication tokens with caching and retry capabilities.
    /// </summary>
    public interface IRackSpaceTokenProvider
    {
        /// <summary>
        /// Gets a valid RackSpace authentication token asynchronously.
        /// Retrieves from cache if available, otherwise authenticates with RackSpace API.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Valid authentication token</returns>
        Task<string> GetTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates the cached token, forcing re-authentication on next request.
        /// </summary>
        void InvalidateCache();
    }
}

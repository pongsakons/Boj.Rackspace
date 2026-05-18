namespace Boj.Rackspace.Infrastructure.Models
{
    /// <summary>
    /// Represents a cached RackSpace authentication token with expiration metadata.
    /// Used to implement intelligent token refresh with expiration buffer.
    /// </summary>
    public class CachedTokenInfo
    {
        /// <summary>
        /// The authentication token value.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Absolute expiration time of the token.
        /// Token is considered expired when UtcNow >= ExpirationTime.
        /// </summary>
        public DateTime ExpirationTime { get; set; }

        /// <summary>
        /// Time when the token refresh should be triggered.
        /// This is ExpirationTime minus the refresh buffer (typically 5 minutes).
        /// </summary>
        public DateTime RefreshTriggerTime { get; set; }

        /// <summary>
        /// When the token was created/refreshed.
        /// Used for logging and diagnostics.
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Determines if token is still valid (not expired).
        /// </summary>
        public bool IsValid => DateTime.UtcNow < ExpirationTime;

        /// <summary>
        /// Determines if token refresh should be triggered.
        /// True when current time is within the buffer window before expiration.
        /// </summary>
        public bool ShouldRefresh => DateTime.UtcNow >= RefreshTriggerTime;

        /// <summary>
        /// Calculates remaining time until token expiration.
        /// </summary>
        public TimeSpan TimeUntilExpiration => ExpirationTime - DateTime.UtcNow;

        /// <summary>
        /// Calculates remaining time until refresh should be triggered.
        /// Negative value means refresh should happen immediately.
        /// </summary>
        public TimeSpan TimeUntilRefresh => RefreshTriggerTime - DateTime.UtcNow;

        /// <summary>
        /// Creates a new CachedTokenInfo with calculated refresh trigger time.
        /// </summary>
        /// <param name="token">The authentication token</param>
        /// <param name="expirationTime">When the token expires</param>
        /// <param name="refreshBufferMinutes">Minutes before expiration to trigger refresh</param>
        /// <returns>New CachedTokenInfo instance</returns>
        public static CachedTokenInfo Create(
            string token,
            DateTime expirationTime,
            int refreshBufferMinutes = 5)
        {
            var now = DateTime.UtcNow;
            return new CachedTokenInfo
            {
                Token = token,
                ExpirationTime = expirationTime,
                RefreshTriggerTime = expirationTime.AddMinutes(-refreshBufferMinutes),
                CreatedUtc = now
            };
        }

        /// <summary>
        /// Gets a summary of token status for logging.
        /// </summary>
        public string GetStatusSummary()
        {
            var status = IsValid ? "Valid" : "Expired";
            var action = ShouldRefresh ? "needs refresh" : "no refresh needed";
            return $"Token: {status}, Age: {DateTime.UtcNow - CreatedUtc:hh\\:mm\\:ss}, Action: {action}";
        }
    }
}
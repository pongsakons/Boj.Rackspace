namespace Boj.Rackspace.Infrastructure.Options
{
    /// <summary>
    /// Configuration options for RackSpace API authentication.
    /// </summary>
    public class RackSpaceTokenOptions
    {
        public const string SectionName = "RackSpace";

        /// <summary>
        /// RackSpace API authentication endpoint.
        /// </summary>
        public string AuthEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// RackSpace username for authentication.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// RackSpace API key for authentication.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Token cache duration in minutes. Default is 24 hours.
        /// </summary>
        public int CacheDurationMinutes { get; set; } = 1440;

        /// <summary>
        /// Maximum number of retry attempts for authentication. Default is 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Initial delay in milliseconds between retry attempts. Default is 500ms.
        /// </summary>
        public int InitialRetryDelayMilliseconds { get; set; } = 500;
    }
}

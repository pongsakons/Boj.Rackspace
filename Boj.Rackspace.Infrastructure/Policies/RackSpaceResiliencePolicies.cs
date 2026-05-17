using Polly;
using Polly.CircuitBreaker;

namespace Boj.Rackspace.Infrastructure.Policies
{
    /// <summary>
    /// Factory for creating Polly resilience policies for RackSpace API authentication.
    /// </summary>
    public static class RackSpaceResiliencePolicies
    {
        /// <summary>
        /// Creates a retry policy with exponential backoff for authentication attempts.
        /// </summary>
        /// <param name="maxRetryAttempts">Maximum number of retry attempts</param>
        /// <param name="initialDelayMilliseconds">Initial delay in milliseconds between retries</param>
        /// <returns>Async policy for HttpRequestMessage</returns>
        public static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(
            int maxRetryAttempts = 3,
            int initialDelayMilliseconds = 500)
        {
            return Policy
                .Handle<HttpRequestException>()
                .Or<OperationCanceledException>()
                .OrResult<HttpResponseMessage>(r =>
                    r.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                    r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                    r.StatusCode == System.Net.HttpStatusCode.InternalServerError ||
                    (int)r.StatusCode == 429) // Too many requests
                .WaitAndRetryAsync(
                    retryCount: maxRetryAttempts,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromMilliseconds(
                            initialDelayMilliseconds * Math.Pow(2, attempt - 1)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var retryMessage = outcome.Exception?.Message ??
                            $"Status: {outcome.Result?.StatusCode}";
                        System.Diagnostics.Debug.WriteLine(
                            $"RackSpace Auth Retry {retryCount}: {retryMessage}. " +
                            $"Retrying in {timespan.TotalMilliseconds:F0}ms");
                    });
        }

        /// <summary>
        /// Creates a circuit breaker policy to prevent cascading failures.
        /// </summary>
        /// <param name="handledEventsAllowedBeforeBreaking">Number of failures before breaking</param>
        /// <param name="durationOfBreakSeconds">Duration in seconds to keep the circuit open</param>
        /// <returns>Async policy for HttpRequestMessage</returns>
        public static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy(
            int handledEventsAllowedBeforeBreaking = 5,
            int durationOfBreakSeconds = 30)
        {
            return Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r =>
                    r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: handledEventsAllowedBeforeBreaking,
                    durationOfBreak: TimeSpan.FromSeconds(durationOfBreakSeconds),
                    onBreak: (outcome, duration) =>
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"RackSpace Auth Circuit Breaker opened for {duration.TotalSeconds:F0}s");
                    },
                    onReset: () =>
                    {
                        System.Diagnostics.Debug.WriteLine("RackSpace Auth Circuit Breaker reset");
                    });
        }

        /// <summary>
        /// Combines retry and circuit breaker policies into a wrap policy.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> CreateCombinedPolicy(
            int maxRetryAttempts = 3,
            int initialDelayMilliseconds = 500,
            int handledEventsAllowedBeforeBreaking = 5,
            int durationOfBreakSeconds = 30)
        {
            var retryPolicy = CreateRetryPolicy(maxRetryAttempts, initialDelayMilliseconds);
            var circuitBreakerPolicy = CreateCircuitBreakerPolicy(
                handledEventsAllowedBeforeBreaking,
                durationOfBreakSeconds);

            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }
    }
}

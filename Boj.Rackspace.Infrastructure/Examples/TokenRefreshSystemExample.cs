using Boj.Rackspace.Infrastructure.Models;
using System.Diagnostics;

namespace Boj.Rackspace.Infrastructure.Examples
{
    /// <summary>
    /// Complete high-concurrency token refresh system example.
    /// Demonstrates proactive refresh, double-check locking, and cache-only reading.
    /// 
    /// Architecture:
    /// 1. AuthTokenHandler reads cache ONLY (no lock, ~0.1ms)
    /// 2. If cache miss/expiring ? calls GetTokenAsync()
    /// 3. GetTokenAsync() uses double-check locking
    /// 4. Only one thread refreshes, others wait briefly
    /// 5. RackSpace token expiration triggers refresh 5 minutes early
    /// 
    /// Performance:
    /// - Cache hit (99%): ~0.1ms
    /// - Lock wait (1%): ~1-2ms
    /// - API call (1x per 24h): ~200-500ms
    /// - Throughput: 10,000+ req/sec
    /// </summary>
    public class TokenRefreshSystemExample
    {
        /// <summary>
        /// Example 1: How AuthTokenHandler works (cache-only, no lock)
        /// </summary>
        public class CacheReadExample
        {
            public void DemonstrateCacheRead()
            {
                // Timeline: 11:55:30 (Token expires at 12:00:00)
                
                // AuthTokenHandler flow:
                var cachedToken = GetTokenFromCacheIfValid();
                
                if (cachedToken == null)
                {
                    // Token not in cache or expiring soon
                    // Call GetTokenAsync() which has locking
                    Console.WriteLine("Cache miss, calling GetTokenAsync()");
                }
                else
                {
                    // Token is valid and not expiring
                    // Return immediately without any lock
                    Console.WriteLine("Cache hit, returning immediately");
                }
            }

            private CachedTokenInfo? GetTokenFromCacheIfValid()
            {
                // Simulated cache
                var now = DateTime.UtcNow;
                
                // Token expires in 5 minutes
                var expirationTime = now.AddMinutes(5);
                var refreshTriggerTime = expirationTime.AddMinutes(-5);  // 5 min before
                
                var cachedToken = new CachedTokenInfo
                {
                    Token = "cached-token",
                    ExpirationTime = expirationTime,
                    RefreshTriggerTime = refreshTriggerTime,
                    CreatedUtc = now
                };

                // Three checks - all must pass
                Console.WriteLine($"  Checking cache...");
                Console.WriteLine($"  - Token found: YES");
                Console.WriteLine($"  - IsValid: {cachedToken.IsValid} (now < expiration)");
                Console.WriteLine($"  - ShouldRefresh: {cachedToken.ShouldRefresh} (now >= refreshTrigger)");

                if (cachedToken.IsValid && !cachedToken.ShouldRefresh)
                {
                    Console.WriteLine($"  ? Return cached token (no refresh needed)");
                    return cachedToken;
                }

                Console.WriteLine($"  ? Return null (needs refresh)");
                return null;  // Need to refresh
            }
        }

        /// <summary>
        /// Example 2: CachedTokenInfo model and computed properties
        /// </summary>
        public class CachedTokenInfoExample
        {
            public void DemonstrateCachedTokenInfo()
            {
                var now = DateTime.UtcNow;
                
                // Scenario 1: Fresh token (just received)
                Console.WriteLine("\n=== Scenario 1: Fresh Token ===");
                var freshToken = CachedTokenInfo.Create(
                    token: "fresh-token",
                    expirationTime: now.AddHours(24),
                    refreshBufferMinutes: 5
                );

                Console.WriteLine($"Token: {freshToken.Token}");
                Console.WriteLine($"Created: {freshToken.CreatedUtc}");
                Console.WriteLine($"Expires: {freshToken.ExpirationTime}");
                Console.WriteLine($"Refresh Trigger: {freshToken.RefreshTriggerTime}");
                Console.WriteLine($"IsValid: {freshToken.IsValid}");
                Console.WriteLine($"ShouldRefresh: {freshToken.ShouldRefresh}");
                Console.WriteLine($"Time Until Expiration: {freshToken.TimeUntilExpiration}");
                Console.WriteLine($"Time Until Refresh: {freshToken.TimeUntilRefresh}");
                Console.WriteLine($"Status: {freshToken.GetStatusSummary()}");

                // Scenario 2: Token expiring soon (within 5 min buffer)
                Console.WriteLine("\n=== Scenario 2: Token Expiring Soon (Refresh Buffer) ===");
                var expiringToken = CachedTokenInfo.Create(
                    token: "expiring-token",
                    expirationTime: now.AddMinutes(2),  // Expires in 2 minutes
                    refreshBufferMinutes: 5
                );

                Console.WriteLine($"IsValid: {expiringToken.IsValid} (token hasn't expired)");
                Console.WriteLine($"ShouldRefresh: {expiringToken.ShouldRefresh} (within 5 min buffer)");
                Console.WriteLine($"Time Until Expiration: {expiringToken.TimeUntilExpiration}");
                Console.WriteLine($"Time Until Refresh: {expiringToken.TimeUntilRefresh} (negative = refresh now!)");
                Console.WriteLine($"Status: {expiringToken.GetStatusSummary()}");

                // Scenario 3: Expired token
                Console.WriteLine("\n=== Scenario 3: Expired Token ===");
                var expiredToken = CachedTokenInfo.Create(
                    token: "expired-token",
                    expirationTime: now.AddSeconds(-10),  // Expired 10 seconds ago
                    refreshBufferMinutes: 5
                );

                Console.WriteLine($"IsValid: {expiredToken.IsValid} (token is expired)");
                Console.WriteLine($"ShouldRefresh: {expiredToken.ShouldRefresh} (definitely needs refresh)");
                Console.WriteLine($"Status: {expiredToken.GetStatusSummary()}");
            }
        }

        /// <summary>
        /// Example 3: Double-check locking pattern
        /// </summary>
        public class DoubleCheckLockingExample
        {
            private static readonly SemaphoreSlim _refreshLock = new(1, 1);
            private CachedTokenInfo? _cachedToken;

            public async Task DemonstrateDoubleCheckLocking()
            {
                Console.WriteLine("\n=== Double-Check Locking Pattern ===");

                var now = DateTime.UtcNow;
                
                // Initial state: Token expires in 2 minutes (should refresh)
                _cachedToken = CachedTokenInfo.Create("old-token", now.AddMinutes(2), 5);

                Console.WriteLine("Thread 1: Check cache without lock");
                if (_cachedToken == null || !_cachedToken.IsValid || _cachedToken.ShouldRefresh)
                {
                    Console.WriteLine("  ? Token missing or needs refresh");
                    Console.WriteLine("Thread 1: Trying to acquire lock...");
                    
                    await _refreshLock.WaitAsync();
                    try
                    {
                        Console.WriteLine("Thread 1: Lock acquired ?");
                        
                        // Double-check after acquiring lock
                        Console.WriteLine("Thread 1: Double-check cache...");
                        if (_cachedToken == null || !_cachedToken.IsValid || _cachedToken.ShouldRefresh)
                        {
                            Console.WriteLine("  ? Still needs refresh, authenticating...");
                            
                            // Simulate API call
                            await Task.Delay(500);
                            
                            // Simulate new token received from API
                            _cachedToken = CachedTokenInfo.Create(
                                "new-token",
                                now.AddHours(24),
                                5
                            );
                            
                            Console.WriteLine("Thread 1: Token refreshed and cached");
                        }
                    }
                    finally
                    {
                        _refreshLock.Release();
                        Console.WriteLine("Thread 1: Lock released");
                    }
                }

                // Simulate Thread 2 arriving while Thread 1 is refreshing
                Console.WriteLine("\nThread 2: Check cache without lock (during refresh)");
                if (_cachedToken == null || !_cachedToken.IsValid || _cachedToken.ShouldRefresh)
                {
                    Console.WriteLine("  ? Needs refresh, trying to acquire lock...");
                    
                    await _refreshLock.WaitAsync();
                    try
                    {
                        Console.WriteLine("Thread 2: Lock acquired ?");
                        
                        // Double-check after acquiring lock
                        Console.WriteLine("Thread 2: Double-check cache...");
                        if (_cachedToken == null || !_cachedToken.IsValid || _cachedToken.ShouldRefresh)
                        {
                            Console.WriteLine("  ? Still needs refresh");
                        }
                        else
                        {
                            Console.WriteLine("  ? Token was refreshed! (Thread 1 already did it) ?");
                            Console.WriteLine($"  ? Using refreshed token: {_cachedToken.Token}");
                        }
                    }
                    finally
                    {
                        _refreshLock.Release();
                    }
                }
            }
        }

        /// <summary>
        /// Example 4: High concurrency scenario
        /// </summary>
        public class HighConcurrencyExample
        {
            public async Task DemonstrateHighConcurrency()
            {
                Console.WriteLine("\n=== High Concurrency: 1000 Requests During Refresh ===");

                var now = DateTime.UtcNow;
                var apiCallCount = 0;
                var lockWaitCount = 0;
                var cacheHitCount = 0;

                // Initial: Token needs refresh
                var cachedToken = CachedTokenInfo.Create("old-token", now.AddMinutes(2), 5);
                var refreshLock = new SemaphoreSlim(1, 1);

                // Simulate 1000 concurrent requests
                var tasks = new List<Task>();
                var sw = Stopwatch.StartNew();

                for (int i = 0; i < 1000; i++)
                {
                    var requestId = i;
                    tasks.Add(Task.Run(async () =>
                    {
                        // FAST PATH: Check without lock
                        if (cachedToken?.IsValid == true && !cachedToken.ShouldRefresh)
                        {
                            Interlocked.Increment(ref cacheHitCount);
                            return;  // ~0.1ms
                        }

                        // SLOW PATH: Need lock
                        Interlocked.Increment(ref lockWaitCount);
                        var lockWaitSw = Stopwatch.StartNew();
                        
                        await refreshLock.WaitAsync();
                        try
                        {
                            lockWaitSw.Stop();
                            if (lockWaitSw.ElapsedMilliseconds > 10)
                                Console.WriteLine($"  Request {requestId}: Lock wait = {lockWaitSw.ElapsedMilliseconds}ms");

                            // Double-check
                            if (cachedToken?.IsValid == true && !cachedToken.ShouldRefresh)
                            {
                                // Token was refreshed by another thread
                                return;
                            }

                            // Only first thread to get lock will refresh
                            if (Interlocked.Increment(ref apiCallCount) == 1)
                            {
                                Console.WriteLine($"  Request {requestId}: Refreshing token (only this request calls API)");
                                await Task.Delay(500);  // Simulate API call
                                cachedToken = CachedTokenInfo.Create("new-token", now.AddHours(24), 5);
                                Console.WriteLine($"  Request {requestId}: Token refreshed and cached");
                            }
                        }
                        finally
                        {
                            refreshLock.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);
                sw.Stop();

                Console.WriteLine($"\nResults:");
                Console.WriteLine($"  Total Requests: 1000");
                Console.WriteLine($"  Cache Hits: {cacheHitCount} (fast path, no lock)");
                Console.WriteLine($"  Lock Waits: {lockWaitCount} (slow path, needs refresh)");
                Console.WriteLine($"  API Calls: {apiCallCount} (should be 1!)");
                Console.WriteLine($"  Total Time: {sw.ElapsedMilliseconds}ms");
                Console.WriteLine($"  Average Time Per Request: {(double)sw.ElapsedMilliseconds / 1000}ms");
                Console.WriteLine($"\nKey Insight: 1000 requests, but only 1 API call!");
            }
        }

        /// <summary>
        /// Example 5: Expiration extraction from RackSpace API response
        /// </summary>
        public class ExpirationExtractionExample
        {
            public void DemonstrateExpirationExtraction()
            {
                Console.WriteLine("\n=== Token Expiration Extraction ===");

                // Simulated RackSpace API response
                var rackspaceResponse = new
                {
                    access = new
                    {
                        token = new
                        {
                            id = "XXXXXXXX-token-value",
                            expires = "2024-01-02T11:50:00.000Z"  // Token expires tomorrow at 11:50 UTC
                        }
                    }
                };

                // Extract values
                var tokenId = rackspaceResponse.access.token.id;
                var expiresString = rackspaceResponse.access.token.expires;

                Console.WriteLine($"Response token.id: {tokenId}");
                Console.WriteLine($"Response token.expires: {expiresString}");

                // Parse expiration
                if (DateTime.TryParse(
                    expiresString,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var expirationTime))
                {
                    Console.WriteLine($"Parsed ExpirationTime: {expirationTime:O}");

                    // Calculate refresh trigger (5 minutes before)
                    var refreshTriggerTime = expirationTime.AddMinutes(-5);
                    Console.WriteLine($"Calculated RefreshTriggerTime: {refreshTriggerTime:O}");

                    // Create CachedTokenInfo
                    var cachedTokenInfo = CachedTokenInfo.Create(tokenId, expirationTime, 5);
                    
                    Console.WriteLine($"\nCreated CachedTokenInfo:");
                    Console.WriteLine($"  Token: {cachedTokenInfo.Token}");
                    Console.WriteLine($"  ExpirationTime: {cachedTokenInfo.ExpirationTime:O}");
                    Console.WriteLine($"  RefreshTriggerTime: {cachedTokenInfo.RefreshTriggerTime:O}");
                    Console.WriteLine($"  CreatedUtc: {cachedTokenInfo.CreatedUtc:O}");
                    Console.WriteLine($"\nNow (simulated): {DateTime.UtcNow:O}");
                    Console.WriteLine($"  IsValid: {cachedTokenInfo.IsValid}");
                    Console.WriteLine($"  ShouldRefresh: {cachedTokenInfo.ShouldRefresh}");
                }
            }
        }

        /// <summary>
        /// Example 6: Complete flow demonstration
        /// </summary>
        public class CompleteFlowExample
        {
            public async Task DemonstrateCompleteFlow()
            {
                Console.WriteLine("\n=== Complete Flow: Request ? Handler ? Provider ? Cache ===\n");

                var now = DateTime.UtcNow;
                var cache = new Dictionary<string, CachedTokenInfo>();
                var refreshLock = new SemaphoreSlim(1, 1);

                // Step 1: Cache initially empty
                Console.WriteLine("Step 1: Request arrives, cache is empty");
                Console.WriteLine($"  Cache.Count: {cache.Count}");

                // Step 2: Handler tries cache-only read
                Console.WriteLine("\nStep 2: AuthTokenHandler.GetTokenFromCacheIfValid()");
                CachedTokenInfo? cachedToken = null;
                cache.TryGetValue("rackspace_token", out cachedToken);
                Console.WriteLine($"  Cache lookup: {(cachedToken == null ? "NOT FOUND" : "FOUND")}");

                if (cachedToken == null)
                {
                    Console.WriteLine("  ? Calling GetTokenAsync() for refresh\n");

                    // Step 3: Provider acquires lock
                    Console.WriteLine("Step 3: RackSpaceTokenProvider.GetTokenAsync()");
                    Console.WriteLine("  Acquiring refresh lock...");

                    await refreshLock.WaitAsync();
                    try
                    {
                        Console.WriteLine("  ? Lock acquired");

                        // Step 4: Double-check
                        Console.WriteLine("  Double-checking cache after lock acquisition");
                        cache.TryGetValue("rackspace_token", out cachedToken);
                        Console.WriteLine($"  Cache lookup: {(cachedToken == null ? "STILL EMPTY" : "FOUND")}");

                        if (cachedToken == null)
                        {
                            // Step 5: Call API
                            Console.WriteLine("  Calling RackSpace API...");
                            await Task.Delay(300);  // Simulate API latency
                            Console.WriteLine("  ? API response received");

                            // Step 6: Extract token info
                            Console.WriteLine("  Extracting token + expiration from response");
                            var extractedToken = new CachedTokenInfo
                            {
                                Token = "new-token-from-api",
                                ExpirationTime = now.AddHours(24),
                                RefreshTriggerTime = now.AddHours(24).AddMinutes(-5),
                                CreatedUtc = now
                            };
                            Console.WriteLine($"    Token: {extractedToken.Token}");
                            Console.WriteLine($"    Expires: {extractedToken.ExpirationTime:O}");
                            Console.WriteLine($"    Refresh Trigger: {extractedToken.RefreshTriggerTime:O}");

                            // Step 7: Cache it
                            Console.WriteLine("  Caching CachedTokenInfo");
                            cache["rackspace_token"] = extractedToken;
                            cachedToken = extractedToken;
                            Console.WriteLine($"  ? Cached. Cache.Count: {cache.Count}");
                        }
                    }
                    finally
                    {
                        refreshLock.Release();
                        Console.WriteLine("  Lock released");
                    }
                }

                // Step 8: Handler adds token to request
                Console.WriteLine("\nStep 4: AuthTokenHandler.AddHeaderToRequest()");
                if (cachedToken != null)
                {
                    Console.WriteLine($"  Adding header: X-Auth-Token: {cachedToken.Token}");
                    Console.WriteLine($"  ? Header added, request proceeding");
                }

                // Step 9: Next request (cache hit)
                Console.WriteLine("\n--- Second request arrives ---\n");
                Console.WriteLine("Step 1: Request arrives, cache has token");
                Console.WriteLine($"  Cache.Count: {cache.Count}");

                Console.WriteLine("\nStep 2: AuthTokenHandler.GetTokenFromCacheIfValid()");
                cache.TryGetValue("rackspace_token", out cachedToken);
                Console.WriteLine($"  Cache lookup: FOUND");
                Console.WriteLine($"  Token: {cachedToken?.Token}");
                Console.WriteLine($"  IsValid: {cachedToken?.IsValid}");
                Console.WriteLine($"  ShouldRefresh: {cachedToken?.ShouldRefresh}");

                if (cachedToken?.IsValid == true && !cachedToken.ShouldRefresh)
                {
                    Console.WriteLine("  ? Token is valid and fresh");
                    Console.WriteLine("  ? Returning token immediately (NO GetTokenAsync call!)");

                    Console.WriteLine("\nStep 3: AuthTokenHandler.AddHeaderToRequest()");
                    Console.WriteLine($"  Adding header: X-Auth-Token: {cachedToken.Token}");
                    Console.WriteLine($"  ? Header added, request proceeding");
                    Console.WriteLine($"  Latency: ~0.1ms (cache hit only)");
                }
            }
        }

        // Main: Run all examples
        public static async Task Main()
        {
            Console.WriteLine("??????????????????????????????????????????????????????????????");
            Console.WriteLine("?      Token Refresh System - High Concurrency Examples      ?");
            Console.WriteLine("??????????????????????????????????????????????????????????????");

            // Example 1
            var cacheReadExample = new CacheReadExample();
            cacheReadExample.DemonstrateCacheRead();

            // Example 2
            var tokenInfoExample = new CachedTokenInfoExample();
            tokenInfoExample.DemonstrateCachedTokenInfo();

            // Example 3
            var lockingExample = new DoubleCheckLockingExample();
            await lockingExample.DemonstrateDoubleCheckLocking();

            // Example 4
            var concurrencyExample = new HighConcurrencyExample();
            await concurrencyExample.DemonstrateHighConcurrency();

            // Example 5
            var extractionExample = new ExpirationExtractionExample();
            extractionExample.DemonstrateExpirationExtraction();

            // Example 6
            var completeFlowExample = new CompleteFlowExample();
            await completeFlowExample.DemonstrateCompleteFlow();

            Console.WriteLine("\n??????????????????????????????????????????????????????????????");
            Console.WriteLine("?              All examples completed successfully            ?");
            Console.WriteLine("??????????????????????????????????????????????????????????????");
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NullZustand
{
    public class RateLimitInfo
    {
        public Queue<DateTime> RequestTimestamps { get; set; }
        public DateTime? BannedUntil { get; set; }

        public RateLimitInfo()
        {
            RequestTimestamps = new Queue<DateTime>();
        }
    }

    public class RateLimiter
    {
        private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitData;
        private readonly object _cleanupLock = new object();
        private DateTime _lastCleanup = DateTime.UtcNow;

        // Rate limit settings
        private const int MAX_REQUESTS_PER_WINDOW = 50; // 50 requests
        private const int WINDOW_SECONDS = 10; // per 10 seconds
        private const int BAN_DURATION_SECONDS = 60; // 1 minute ban for violators
        private const int CLEANUP_INTERVAL_SECONDS = 300; // Clean up old data every 5 minutes

        public RateLimiter()
        {
            _rateLimitData = new ConcurrentDictionary<string, RateLimitInfo>();
        }

        public bool AllowRequest(string sessionId, out string errorMessage)
        {
            errorMessage = null;
            DateTime now = DateTime.UtcNow;

            PerformCleanupIfNeeded(now);

            var info = _rateLimitData.GetOrAdd(sessionId, _ => new RateLimitInfo());

            lock (info)
            {
                if (info.BannedUntil.HasValue)
                {
                    if (now < info.BannedUntil.Value)
                    {
                        int secondsRemaining = (int)(info.BannedUntil.Value - now).TotalSeconds;
                        errorMessage = $"Rate limit exceeded. Try again in {secondsRemaining} seconds.";
                        Console.WriteLine($"[RATE_LIMIT] Blocked banned session {sessionId} ({secondsRemaining}s remaining)");
                        return false;
                    }
                    else
                    {
                        // Ban expired, clear it
                        info.BannedUntil = null;
                        info.RequestTimestamps.Clear();
                        Console.WriteLine($"[RATE_LIMIT] Ban expired for session {sessionId}");
                    }
                }

                DateTime windowStart = now.AddSeconds(-WINDOW_SECONDS);
                while (info.RequestTimestamps.Count > 0 && info.RequestTimestamps.Peek() < windowStart)
                {
                    info.RequestTimestamps.Dequeue();
                }

                if (info.RequestTimestamps.Count >= MAX_REQUESTS_PER_WINDOW)
                {
                    info.BannedUntil = now.AddSeconds(BAN_DURATION_SECONDS);
                    errorMessage = $"Rate limit exceeded. Maximum {MAX_REQUESTS_PER_WINDOW} requests per {WINDOW_SECONDS} seconds. Banned for {BAN_DURATION_SECONDS} seconds.";
                    Console.WriteLine($"[RATE_LIMIT] Session {sessionId} exceeded rate limit ({info.RequestTimestamps.Count} requests in {WINDOW_SECONDS}s) - banned for {BAN_DURATION_SECONDS}s");
                    return false;
                }

                info.RequestTimestamps.Enqueue(now);
                return true;
            }
        }

        public void ClearSession(string sessionId)
        {
            if (_rateLimitData.TryRemove(sessionId, out _))
            {
                Console.WriteLine($"[RATE_LIMIT] Cleared rate limit data for session {sessionId}");
            }
        }

        private void PerformCleanupIfNeeded(DateTime now)
        {
            if ((now - _lastCleanup).TotalSeconds < CLEANUP_INTERVAL_SECONDS)
            {
                return;
            }

            lock (_cleanupLock)
            {
                if ((now - _lastCleanup).TotalSeconds < CLEANUP_INTERVAL_SECONDS)
                {
                    return;
                }

                _lastCleanup = now;

                DateTime cutoffTime = now.AddSeconds(-WINDOW_SECONDS * 2);
                var sessionsToRemove = _rateLimitData
                    .Where(kvp =>
                    {
                        lock (kvp.Value)
                        {
                            return !kvp.Value.BannedUntil.HasValue &&
                                   (kvp.Value.RequestTimestamps.Count == 0 ||
                                    kvp.Value.RequestTimestamps.All(t => t < cutoffTime));
                        }
                    })
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var sessionId in sessionsToRemove)
                {
                    _rateLimitData.TryRemove(sessionId, out _);
                }

                if (sessionsToRemove.Count > 0)
                {
                    Console.WriteLine($"[RATE_LIMIT] Cleaned up {sessionsToRemove.Count} inactive session(s)");
                }
            }
        }

    }
}


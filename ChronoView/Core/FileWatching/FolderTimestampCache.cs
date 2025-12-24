using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// Thread-safe cache for folder timestamps with automatic TTL-based cleanup.
    /// Used to cache timestamps extracted from Normal folder names to avoid
    /// repeated extraction and file system access.
    /// </summary>
    public class FolderTimestampCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly int _ttlSeconds;
        private readonly ILogger _logger;

        private class CacheEntry
        {
            public DateTime Timestamp { get; set; }
            public DateTime CachedAt { get; set; }
        }

        /// <summary>
        /// Gets the current number of entries in the cache.
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Initializes a new instance of the FolderTimestampCache.
        /// </summary>
        /// <param name="ttlSeconds">Time-to-live for cache entries in seconds. Default is 300 (5 minutes).</param>
        /// <param name="logger">Logger instance.</param>
        public FolderTimestampCache(int ttlSeconds = 300, ILogger<FolderTimestampCache>? logger = null)
        {
            _cache = new ConcurrentDictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
            _ttlSeconds = ttlSeconds;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<FolderTimestampCache>.Instance;
        }

        /// <summary>
        /// Adds or updates a timestamp for the specified folder path.
        /// </summary>
        /// <param name="folderPath">Absolute path to the folder.</param>
        /// <param name="timestamp">Timestamp to cache.</param>
        /// <exception cref="ArgumentNullException">Thrown if folderPath is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown if timestamp is default value.</exception>
        public void Add(string folderPath, DateTime timestamp)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentNullException(nameof(folderPath));
            }

            if (timestamp == default)
            {
                throw new ArgumentException("Invalid timestamp", nameof(timestamp));
            }

            var entry = new CacheEntry
            {
                Timestamp = timestamp,
                CachedAt = DateTime.UtcNow
            };

            _cache.AddOrUpdate(folderPath, entry, (key, existing) => entry);

            _logger.LogDebug("Cached timestamp for {Path}: {Timestamp}", 
                folderPath, timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

            // Trigger cleanup if cache is getting large
            if (_cache.Count > 1000)
            {
                _ = System.Threading.Tasks.Task.Run(() => CleanupExpiredEntries());
            }
        }

        /// <summary>
        /// Tries to get a cached timestamp for the specified folder path.
        /// Returns false if the entry doesn't exist or has expired.
        /// </summary>
        /// <param name="folderPath">Absolute path to the folder.</param>
        /// <param name="timestamp">When this method returns, contains the cached timestamp if found and not expired; otherwise, default(DateTime).</param>
        /// <returns>true if a valid (non-expired) timestamp was found; otherwise, false.</returns>
        public bool TryGet(string folderPath, out DateTime timestamp)
        {
            timestamp = default;

            if (string.IsNullOrEmpty(folderPath))
            {
                return false;
            }

            if (_cache.TryGetValue(folderPath, out CacheEntry? entry))
            {
                // Check if expired
                TimeSpan age = DateTime.UtcNow - entry.CachedAt;

                if (age.TotalSeconds > _ttlSeconds)
                {
                    // Expired - remove and return false
                    _cache.TryRemove(folderPath, out _);
                    _logger.LogDebug("Cache entry expired for {Path}", folderPath);
                    return false;
                }

                // Valid entry found
                timestamp = entry.Timestamp;
                return true;
            }

            // Not found
            return false;
        }

        /// <summary>
        /// Removes the cached timestamp for the specified folder path.
        /// </summary>
        /// <param name="folderPath">Absolute path to the folder.</param>
        /// <returns>true if the entry was removed; false if it didn't exist.</returns>
        public bool Remove(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return false;
            }

            return _cache.TryRemove(folderPath, out _);
        }

        /// <summary>
        /// Clears all entries from the cache.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            _logger.LogInformation("Cache cleared");
        }

        /// <summary>
        /// Removes expired entries from the cache.
        /// Called automatically when cache grows beyond 1000 entries.
        /// </summary>
        private void CleanupExpiredEntries()
        {
            DateTime cutoff = DateTime.UtcNow.AddSeconds(-_ttlSeconds);
            int removed = 0;

            foreach (var kvp in _cache)
            {
                if (kvp.Value.CachedAt < cutoff)
                {
                    if (_cache.TryRemove(kvp.Key, out _))
                    {
                        removed++;
                    }
                }
            }

            if (removed > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired cache entries", removed);
            }
        }
    }
}

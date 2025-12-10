using System.Collections.Concurrent;

namespace ChronoView.Core.ImageProcessing;

/// <summary>
/// Thread-safe LRU (Least Recently Used) cache implementation.
/// </summary>
/// <typeparam name="TKey">The type of keys in the cache.</typeparam>
/// <typeparam name="TValue">The type of values in the cache.</typeparam>
public class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly long _maxSizeBytes;
    private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new();
    private readonly LinkedList<TKey> _lruList = new();
    private readonly object _lock = new();
    private long _currentSizeBytes;

    public LruCache(long maxSizeBytes)
    {
        if (maxSizeBytes <= 0)
            throw new ArgumentException("Max size must be positive", nameof(maxSizeBytes));

        _maxSizeBytes = maxSizeBytes;
    }

    /// <summary>
    /// Tries to get a value from the cache.
    /// </summary>
    public bool TryGet(TKey key, out TValue? value)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            lock (_lock)
            {
                // Move to front of LRU list (most recently used)
                _lruList.Remove(entry.Node);
                entry.Node = _lruList.AddFirst(key);
            }

            value = entry.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Adds or updates a value in the cache.
    /// </summary>
    public void Add(TKey key, TValue value)
    {
        var sizeBytes = EstimateSize(value);

        lock (_lock)
        {
            // Remove existing entry if present
            if (_cache.TryRemove(key, out var existingEntry))
            {
                _lruList.Remove(existingEntry.Node);
                _currentSizeBytes -= existingEntry.SizeBytes;
            }

            // Evict least recently used items if necessary
            while (_currentSizeBytes + sizeBytes > _maxSizeBytes && _lruList.Count > 0)
            {
                var lruKey = _lruList.Last!.Value;
                if (_cache.TryRemove(lruKey, out var lruEntry))
                {
                    _lruList.RemoveLast();
                    _currentSizeBytes -= lruEntry.SizeBytes;
                }
            }

            // Add new entry
            var node = _lruList.AddFirst(key);
            var entry = new CacheEntry(value, node, sizeBytes);
            _cache[key] = entry;
            _currentSizeBytes += sizeBytes;
        }
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _lruList.Clear();
            _currentSizeBytes = 0;
        }
    }

    /// <summary>
    /// Gets the current cache size in bytes.
    /// </summary>
    public long GetSizeBytes()
    {
        lock (_lock)
        {
            return _currentSizeBytes;
        }
    }

    /// <summary>
    /// Gets the number of items in the cache.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }

    private long EstimateSize(TValue value)
    {
        return value switch
        {
            byte[] bytes => bytes.Length,
            string str => str.Length * 2, // Approximate size for string
            _ => 1024 // Default estimate for other types
        };
    }

    private class CacheEntry
    {
        public TValue Value { get; }
        public LinkedListNode<TKey> Node { get; set; }
        public long SizeBytes { get; }

        public CacheEntry(TValue value, LinkedListNode<TKey> node, long sizeBytes)
        {
            Value = value;
            Node = node;
            SizeBytes = sizeBytes;
        }
    }
}

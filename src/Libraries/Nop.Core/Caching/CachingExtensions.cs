namespace Nop.Core.Caching;

public static class CachingExtensions
{
    /// <summary>
    /// Get a cached item. If it's not in the cache yet, then load and cache it.
    /// NOTE: this method is only kept for backwards compatibility: the async overload is preferred!
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="cacheManager">Cache manager</param>
    /// <param name="key">Cache key</param>
    /// <param name="acquire">Function to load item if it's not in the cache yet</param>
    /// <returns>The cached value associated with the specified key</returns>
    public static T Get<T>(this IStaticCacheManager cacheManager, CacheKey key, Func<T> acquire)
    {
        return cacheManager.GetAsync(key, acquire).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get cached items for several keys. The ones not in the cache yet are loaded with a single call and cached,
    /// each under the same key and with the same type as the per-item method caches them, so both share the cache.
    /// The cache itself is still read key by key; only the load of the missing items is batched
    /// </summary>
    /// <typeparam name="TKey">Type of the item identifier</typeparam>
    /// <typeparam name="T">Type of cached item; must match the type cached by the per-item method</typeparam>
    /// <param name="cacheManager">Cache manager</param>
    /// <param name="keys">Item identifiers</param>
    /// <param name="prepareKey">Function to prepare the cache key of an item</param>
    /// <param name="acquireMissing">Function to load the items that are not in the cache yet</param>
    /// <param name="ifNotLoaded">Function to create the value cached for an item the load did not return; pass null to skip such items</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the cached values by item identifier
    /// </returns>
    public static async Task<IDictionary<TKey, T>> GetManyAsync<TKey, T>(this IStaticCacheManager cacheManager,
        IEnumerable<TKey> keys, Func<TKey, CacheKey> prepareKey,
        Func<TKey[], Task<IDictionary<TKey, T>>> acquireMissing, Func<TKey, T> ifNotLoaded = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(prepareKey);
        ArgumentNullException.ThrowIfNull(acquireMissing);

        var result = new Dictionary<TKey, T>();
        var missing = new List<(TKey Key, CacheKey CacheKey)>();

        foreach (var key in keys.Distinct())
        {
            var cacheKey = prepareKey(key);
            var cached = await cacheManager.GetAsync(cacheKey, default(T));

            if (cached != null)
                result[key] = cached;
            else
                missing.Add((key, cacheKey));
        }

        if (!missing.Any())
            return result;

        var loaded = await acquireMissing(missing.Select(item => item.Key).ToArray());

        foreach (var (key, cacheKey) in missing)
        {
            var value = loaded.TryGetValue(key, out var item) ? item : ifNotLoaded?.Invoke(key);
            if (value == null)
                continue;

            //cache through the same "get or load" call the per-item method uses, so a value cached meanwhile wins
            result[key] = await cacheManager.GetAsync<T>(cacheKey, () => value);
        }

        return result;
    }

    /// <summary>
    /// Remove items by cache key prefix
    /// </summary>
    /// <param name="cacheManager">Cache manager</param>
    /// <param name="prefix">Cache key prefix</param>
    /// <param name="prefixParameters">Parameters to create cache key prefix</param>
    public static void RemoveByPrefix(this IStaticCacheManager cacheManager, string prefix, params object[] prefixParameters)
    {
        cacheManager.RemoveByPrefixAsync(prefix, prefixParameters).Wait();
    }
}
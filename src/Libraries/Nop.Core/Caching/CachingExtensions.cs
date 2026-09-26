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

        return await GetManyCoreAsync(keys,
            key => cacheManager.GetAsync(prepareKey(key), default(T)),
            //cache through the same "get or load" call the per-item method uses, so a value cached meanwhile wins
            (key, value) => cacheManager.GetAsync<T>(prepareKey(key), () => value),
            acquireMissing, ifNotLoaded);
    }

    /// <summary>
    /// Get items cached for the current request for several keys. The ones not in the cache yet are loaded with a single
    /// call and cached, each under the same key and with the same type as the per-item method caches them, so both share
    /// the cache. The cache itself is still read key by key; only the load of the missing items is batched
    /// </summary>
    /// <typeparam name="TKey">Type of the item identifier</typeparam>
    /// <typeparam name="T">Type of cached item; must match the type cached by the per-item method</typeparam>
    /// <param name="cacheManager">Short term cache manager</param>
    /// <param name="keys">Item identifiers</param>
    /// <param name="cacheKey">Initial cache key</param>
    /// <param name="cacheKeyParameters">Function to get the parameters of the cache key of an item</param>
    /// <param name="acquireMissing">Function to load the items that are not in the cache yet</param>
    /// <param name="ifNotLoaded">Function to create the value cached for an item the load did not return; pass null to skip such items</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the cached values by item identifier
    /// </returns>
    public static async Task<IDictionary<TKey, T>> GetManyAsync<TKey, T>(this IShortTermCacheManager cacheManager,
        IEnumerable<TKey> keys, CacheKey cacheKey, Func<TKey, object[]> cacheKeyParameters,
        Func<TKey[], Task<IDictionary<TKey, T>>> acquireMissing, Func<TKey, T> ifNotLoaded = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(cacheKey);
        ArgumentNullException.ThrowIfNull(cacheKeyParameters);
        ArgumentNullException.ThrowIfNull(acquireMissing);

        return await GetManyCoreAsync(keys,
            //the short term cache has no "try get"; an acquire returning null only reads, because null values are not cached
            key => cacheManager.GetAsync(() => Task.FromResult<T>(null), cacheKey, cacheKeyParameters(key)),
            (key, value) => cacheManager.GetAsync(() => Task.FromResult(value), cacheKey, cacheKeyParameters(key)),
            acquireMissing, ifNotLoaded);
    }

    /// <summary>
    /// Get cached items for several keys, loading the missing ones with a single call; the part of GetManyAsync
    /// shared by the cache managers, which differ only in how a single item is read from and written to the cache
    /// </summary>
    /// <typeparam name="TKey">Type of the item identifier</typeparam>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="keys">Item identifiers</param>
    /// <param name="getCached">Function to get the cached item, or null when it is not cached</param>
    /// <param name="cache">Function to cache an item and return the cached value</param>
    /// <param name="acquireMissing">Function to load the items that are not in the cache yet</param>
    /// <param name="ifNotLoaded">Function to create the value cached for an item the load did not return; pass null to skip such items</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the cached values by item identifier
    /// </returns>
    private static async Task<IDictionary<TKey, T>> GetManyCoreAsync<TKey, T>(IEnumerable<TKey> keys,
        Func<TKey, Task<T>> getCached, Func<TKey, T, Task<T>> cache,
        Func<TKey[], Task<IDictionary<TKey, T>>> acquireMissing, Func<TKey, T> ifNotLoaded) where T : class
    {
        var result = new Dictionary<TKey, T>();
        var missing = new List<TKey>();

        foreach (var key in keys.Distinct())
        {
            var cached = await getCached(key);

            if (cached != null)
                result[key] = cached;
            else
                missing.Add(key);
        }

        if (!missing.Any())
            return result;

        var loaded = await acquireMissing(missing.ToArray());

        foreach (var key in missing)
        {
            var value = loaded.TryGetValue(key, out var item) ? item : ifNotLoaded?.Invoke(key);
            if (value == null)
                continue;

            result[key] = await cache(key, value);
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
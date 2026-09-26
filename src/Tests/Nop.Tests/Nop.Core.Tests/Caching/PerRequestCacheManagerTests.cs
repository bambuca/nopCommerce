using FluentAssertions;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using NUnit.Framework;

namespace Nop.Tests.Nop.Core.Tests.Caching;

[TestFixture]
public class PerRequestCacheManagerTests : BaseNopTest
{
    private CacheKey _cacheKey;
    private IShortTermCacheManager _shortTermCacheManager;

    [SetUp]
    public void SetUp()
    {
        //CacheKey reads AppSettings, so it is created only once the base test has set them up
        _cacheKey = new CacheKey("Nop.test.many.{0}");
        //a fresh manager per test, as each request gets its own
        _shortTermCacheManager = new PerRequestCacheManager(Singleton<AppSettings>.Instance);
    }

    private static object[] KeyParameters(int id) => new object[] { id };

    [Test]
    public async Task GetManyReturnsCachedItemsAndLoadsOnlyTheMissingOnesWithOneCall()
    {
        var cached = await _shortTermCacheManager.GetAsync(() => Task.FromResult(new List<int> { 1 }), _cacheKey, 1);
        var calls = new List<int[]>();

        var result = await _shortTermCacheManager.GetManyAsync<int, List<int>>(new[] { 1, 2, 3, 2 }, _cacheKey, KeyParameters,
            missing =>
            {
                calls.Add(missing);
                return Task.FromResult<IDictionary<int, List<int>>>(new Dictionary<int, List<int>> { [2] = new() { 2 } });
            },
            _ => new List<int>());

        calls.Should().ContainSingle().Which.Should().Equal(2, 3);
        result[1].Should().BeSameAs(cached);
        result[2].Should().Equal(2);
        result[3].Should().BeEmpty("items the load did not return get the fallback value");

        //loaded items are cached under their keys, so the per-item call is served from the cache
        (await _shortTermCacheManager.GetAsync(() => Task.FromResult(new List<int> { 99 }), _cacheKey, 3)).Should().BeSameAs(result[3]);
    }

    [Test]
    public async Task GetManySkipsItemsTheLoadDidNotReturnWhenThereIsNoFallback()
    {
        var result = await _shortTermCacheManager.GetManyAsync<int, List<int>>(new[] { 1 }, _cacheKey, KeyParameters,
            _ => Task.FromResult<IDictionary<int, List<int>>>(new Dictionary<int, List<int>>()));

        result.Should().BeEmpty();

        //nothing was cached, so a per-item call loads the item
        (await _shortTermCacheManager.GetAsync(() => Task.FromResult(new List<int> { 7 }), _cacheKey, 1)).Should().Equal(7);
    }
}

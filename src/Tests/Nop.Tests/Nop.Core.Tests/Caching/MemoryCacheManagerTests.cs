using FluentAssertions;
using Nop.Core.Caching;
using NUnit.Framework;

namespace Nop.Tests.Nop.Core.Tests.Caching;

[TestFixture]
public class MemoryCacheManagerTests : BaseNopTest
{
    private MemoryCacheManager _staticCacheManager;

    [OneTimeSetUp]
    public void Setup()
    {
        _staticCacheManager = GetService<IStaticCacheManager>() as MemoryCacheManager;
    }

    [TearDown]
    public async Task TaskTearDown()
    {
        await _staticCacheManager.ClearAsync();
    }

    [Test]
    public async Task CanSetAndGetObjectFromCache()
    {
        await _staticCacheManager.SetAsync(new CacheKey("some_key_1"), 3);
        var rez = await _staticCacheManager.GetAsync(new CacheKey("some_key_1"), () => 0);
        rez.Should().Be(3);
    }

    [Test]
    public async Task DoesNotIgnoreKeyCase()
    {
        await _staticCacheManager.SetAsync(new CacheKey("Some_Key_1"), 3);
        var rez = await _staticCacheManager.GetAsync(new CacheKey("some_key_1"), () => 0);
        rez.Should().Be(0);
    }

    [Test]
    public async Task CanValidateWhetherObjectIsCached()
    {
        await _staticCacheManager.SetAsync(new CacheKey("some_key_1"), 3);
        await _staticCacheManager.SetAsync(new CacheKey("some_key_2"), 4);

        var rez = await _staticCacheManager.GetAsync(new CacheKey("some_key_1"), () => 2);
        rez.Should().Be(3);
        rez = await _staticCacheManager.GetAsync(new CacheKey("some_key_2"), () => 2);
        rez.Should().Be(4);
    }

    [Test]
    public async Task CanClearCache()
    {
        await _staticCacheManager.SetAsync(new CacheKey("some_key_1"), 3);

        await _staticCacheManager.ClearAsync();

        var rez = await _staticCacheManager.GetAsync<object>(new CacheKey("some_key_1"));
        rez.Should().BeNull();
    }

    [Test]
    public async Task GetReturnsValueIfSet()
    {
        var key = new CacheKey("some_key_1");
        await _staticCacheManager.SetAsync(key, 3);
        var res = await _staticCacheManager.GetAsync<int>(key);
        res.Should().Be(3);
    }

    [Test]
    public async Task GetReturnsDefaultIfNotSet()
    {
        var key = new CacheKey("some_key_1");
        var res = await _staticCacheManager.GetAsync(key, 1);
        res.Should().Be(1);
        res = await _staticCacheManager.GetAsync<int>(key);
        res.Should().Be(0);
    }

    [Test]
    public async Task CanRemoveByPrefix()
    {
        await _staticCacheManager.SetAsync(new CacheKey("some_key_1"), 1);
        await _staticCacheManager.SetAsync(new CacheKey("some_key_2"), 2);
        await _staticCacheManager.SetAsync(new CacheKey("some_other_key"), 3);

        await _staticCacheManager.RemoveByPrefixAsync("some_key");

        var result = await _staticCacheManager.GetAsync(new CacheKey("some_key_1"), 0);
        result.Should().Be(0);
        result = await _staticCacheManager.GetAsync(new CacheKey("some_key_2"), 0);
        result.Should().Be(0);
        result = await _staticCacheManager.GetAsync(new CacheKey("some_other_key"), 0);
        result.Should().Be(3);
    }

    [Test]
    public async Task ExecutesSetInOrder()
    {
        await Task.WhenAll(Enumerable.Range(1, 5).Select(i => _staticCacheManager.SetAsync(new CacheKey("some_key_1"), i)));
        var value = await _staticCacheManager.GetAsync(new CacheKey("some_key_1"), 0);
        value.Should().Be(5);
    }

    [Test]
    public async Task GetsLazily()
    {
        var xs = new int[5];
        await Task.WhenAll(xs.Select((_, i) => _staticCacheManager.GetAsync(
            new CacheKey("some_key_1"),
            async () =>
            {
                xs[i] = 1;
                await Task.Delay(10);
                return i;
            })));
        var value = await _staticCacheManager.GetAsync(new CacheKey("some_key_1"), () => Task.FromResult(-1));
        value.Should().Be(0);
        xs.Sum().Should().Be(1);
    }

    [Test]
    public async Task SholThrowsExceptionButNotCacheIt()
    {
        var cacheKey = new CacheKey("some_key_1");

        Assert.ThrowsAsync<ApplicationException>(() => _staticCacheManager.GetAsync(
            cacheKey,
            Task<object> () => throw new ApplicationException()));

        //should not cache exception
        var rez = await _staticCacheManager.GetAsync(cacheKey, Task<object> () => Task.FromResult((object)1));
        rez.Should().Be(1);

        await _staticCacheManager.RemoveAsync(cacheKey);

        Assert.ThrowsAsync<ApplicationException>(() => _staticCacheManager.GetAsync(
            cacheKey,
            Task<object> () => throw new ApplicationException()));

        //should not cache exception
        rez = await _staticCacheManager.GetAsync(cacheKey, (object)1);
        rez.Should().Be(1);

        await _staticCacheManager.RemoveAsync(cacheKey);

        Assert.ThrowsAsync<ApplicationException>(() => _staticCacheManager.GetAsync<object>(
            cacheKey,
            () => throw new ApplicationException()));

        //should not cache exception
        rez = await _staticCacheManager.GetAsync(cacheKey, () => (object)1);
        rez.Should().Be(1);

        await _staticCacheManager.RemoveAsync(cacheKey);

        Assert.ThrowsAsync<ApplicationException>(() => _staticCacheManager.GetAsync<object>(
            cacheKey,
            () => throw new ApplicationException()));

        //should not cache exception
        rez = await _staticCacheManager.GetAsync(cacheKey);
        rez.Should().BeNull();
    }

    [Test]
    public async Task CanGetAsObject()
    {
        var key = new CacheKey("some_key_1");
        await _staticCacheManager.SetAsync(key, 1);
        var obj = await _staticCacheManager.GetAsync(key);
        obj.Should().Be(1);
        obj = await _staticCacheManager.GetAsync(new CacheKey("some_key_2"));
        obj.Should().BeNull();
    }

    [Test]
    public async Task CanGetNullButNotCacheIt()
    {
        var key = new CacheKey("some_key_1");

        var obj = await _staticCacheManager.GetAsync<object>(
            key, () => null);

        obj.Should().BeNull();

        obj = await _staticCacheManager.GetAsync(
            key, Task<object> () => Task.FromResult((object)null));

        obj.Should().BeNull();

        obj = await _staticCacheManager.GetAsync<object>(key, () => 1);
        obj.Should().Be(1);

        obj = await _staticCacheManager.GetAsync<object>(key, () => null);
        obj.Should().Be(1);

        obj = await _staticCacheManager.GetAsync(
            key, Task<object> () => Task.FromResult((object)null));

        obj.Should().Be(1);

        await _staticCacheManager.RemoveAsync(key);

        await _staticCacheManager.SetAsync<object>(key, null);

        obj = await _staticCacheManager.GetAsync<int>(key);
        obj.Should().Be(0);
    }

    [Test]
    public async Task GetManyReturnsCachedItemsAndLoadsOnlyTheMissingOnesWithOneCall()
    {
        static CacheKey keyOf(int id) => new($"many_key_{id}");
        var cached = new List<int> { 1 };
        await _staticCacheManager.SetAsync(keyOf(1), cached);
        var calls = new List<int[]>();

        var result = await _staticCacheManager.GetManyAsync<int, List<int>>(new[] { 1, 2, 3, 2 }, keyOf,
            missing =>
            {
                calls.Add(missing);
                return Task.FromResult<IDictionary<int, List<int>>>(new Dictionary<int, List<int>> { [2] = new() { 2 } });
            },
            _ => new List<int>());

        calls.Should().ContainSingle().Which.Should().Equal(2, 3);
        result.Should().HaveCount(3);
        result[1].Should().BeSameAs(cached);
        result[2].Should().Equal(2);
        result[3].Should().BeEmpty("items the load did not return get the fallback value");

        //loaded items are cached under their keys, so the per-item call is served from the cache
        (await _staticCacheManager.GetAsync(keyOf(2), () => new List<int> { 99 })).Should().BeSameAs(result[2]);
        (await _staticCacheManager.GetAsync(keyOf(3), () => new List<int> { 99 })).Should().BeSameAs(result[3]);
    }

    [Test]
    public async Task GetManyDoesNotLoadWhenEverythingIsCached()
    {
        await _staticCacheManager.SetAsync(new CacheKey("many_cached_1"), new List<int> { 1 });

        var result = await _staticCacheManager.GetManyAsync<int, List<int>>(new[] { 1 }, id => new CacheKey($"many_cached_{id}"),
            _ => throw new InvalidOperationException("nothing should be loaded"));

        result[1].Should().Equal(1);
    }

    [Test]
    public async Task GetManySkipsItemsTheLoadDidNotReturnWhenThereIsNoFallback()
    {
        var result = await _staticCacheManager.GetManyAsync<int, List<int>>(new[] { 1 }, id => new CacheKey($"many_skip_{id}"),
            _ => Task.FromResult<IDictionary<int, List<int>>>(new Dictionary<int, List<int>>()));

        result.Should().BeEmpty();
        (await _staticCacheManager.GetAsync(new CacheKey("many_skip_1"), default(List<int>))).Should().BeNull();
    }
}
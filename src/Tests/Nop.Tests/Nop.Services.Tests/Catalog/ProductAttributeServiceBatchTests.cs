using FluentAssertions;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Catalog;

[TestFixture]
public class ProductAttributeServiceBatchTests : ServiceTest
{
    private static readonly int[] _productIds = Enumerable.Range(1, 30).ToArray();

    private IProductAttributeService _productAttributeService;
    private IStaticCacheManager _staticCacheManager;

    [OneTimeSetUp]
    public void SetUp()
    {
        _productAttributeService = GetService<IProductAttributeService>();
        _staticCacheManager = GetService<IStaticCacheManager>();
    }

    [SetUp]
    [TearDown]
    public async Task ClearCache()
    {
        await _staticCacheManager.RemoveByPrefixAsync(NopEntityCacheDefaults<ProductAttributeMapping>.Prefix);
    }

    [Test]
    public async Task CanGetMappingsForManyProductsWithOneQueryAndSeedTheCache()
    {
        var byProduct = await _productAttributeService.GetProductAttributeMappingsByProductIdsAsync([.. _productIds, 0, 1]);

        byProduct.Should().HaveCount(_productIds.Length, "invalid and duplicate identifiers are skipped");
        byProduct.Values.Should().Contain(mappings => mappings.Count > 1, "the sample data must contain a product with several attributes");

        foreach (var productId in _productIds)
        {
            //the per-product method must be served from the seeded cache, i.e. return the very same list
            var single = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(productId);
            single.Should().BeSameAs(byProduct[productId]);
        }
    }

    [Test]
    public async Task MappingsAreOrderedAsByThePerProductCall()
    {
        var expected = new Dictionary<int, List<int>>();
        foreach (var productId in _productIds)
            expected[productId] = (await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(productId)).Select(m => m.Id).ToList();
        await ClearCache();

        var byProduct = await _productAttributeService.GetProductAttributeMappingsByProductIdsAsync(_productIds);

        foreach (var productId in _productIds)
            byProduct[productId].Select(m => m.Id).Should().Equal(expected[productId]);
    }

    [Test]
    public async Task CachedMappingsAreReturnedWithoutReload()
    {
        var single = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(1);

        (await _productAttributeService.GetProductAttributeMappingsByProductIdsAsync([1]))
            .Should().ContainSingle().Which.Value.Should().BeSameAs(single);
    }

    [Test]
    public async Task NullInputIsRejected()
    {
        await _productAttributeService.Invoking(s => s.GetProductAttributeMappingsByProductIdsAsync(null))
            .Should().ThrowAsync<ArgumentNullException>();
    }
}

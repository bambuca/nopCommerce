using FluentAssertions;
using Nop.Services.Media;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Media;

[TestFixture]
public class PictureServiceBatchTests : ServiceTest
{
    private static readonly int[] _productIds = Enumerable.Range(1, 30).ToArray();

    private IPictureService _pictureService;

    [OneTimeSetUp]
    public void SetUp()
    {
        _pictureService = GetService<IPictureService>();
    }

    [Test]
    public async Task GetPicturesByProductIdsReturnsTheSamePicturesAsThePerProductCall()
    {
        var byProduct = await _pictureService.GetPicturesByProductIdsAsync([.. _productIds, 0, 1]);

        byProduct.Values.Should().Contain(pictures => pictures.Count > 1, "the sample data must contain a product with several pictures");

        foreach (var productId in _productIds)
        {
            var single = (await _pictureService.GetPicturesByProductIdAsync(productId)).Select(p => p.Id).ToList();

            if (single.Count > 0)
                byProduct[productId].Select(p => p.Id).Should().Equal(single, "the order is the same as of the per-product call");
            else
                byProduct.Should().NotContainKey(productId, "products without pictures are missing");
        }
    }

    [Test]
    public async Task GetPicturesByProductIdsLimitsTheRecordsPerProduct()
    {
        var byProduct = await _pictureService.GetPicturesByProductIdsAsync(_productIds, 1);

        byProduct.Should().NotBeEmpty();
        foreach (var (productId, pictures) in byProduct)
        {
            var first = await _pictureService.GetPicturesByProductIdAsync(productId, 1);
            pictures.Select(p => p.Id).Should().Equal(first.Select(p => p.Id));
        }
    }

    [Test]
    public async Task GetPicturesByProductIdsIgnoresEmptyInput()
    {
        (await _pictureService.GetPicturesByProductIdsAsync([])).Should().BeEmpty();
        (await _pictureService.GetPicturesByProductIdsAsync([0])).Should().BeEmpty();
        await _pictureService.Invoking(s => s.GetPicturesByProductIdsAsync(null)).Should().ThrowAsync<ArgumentNullException>();
    }
}

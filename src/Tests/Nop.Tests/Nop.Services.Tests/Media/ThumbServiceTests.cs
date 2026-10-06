using FluentAssertions;
using Nop.Core.Domain.Media;
using Nop.Services.Media;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Media;

[TestFixture]
public class ThumbServiceTests : ServiceTest
{
    private ThumbService _thumbService;
    private readonly List<string> _thumbPaths = [];

    [OneTimeSetUp]
    public void SetUp()
    {
        //thumbs info is available on the file system implementation only (see CommonController.Maintenance)
        _thumbService = (ThumbService)GetService<IThumbService>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var path in _thumbPaths.Where(File.Exists))
            File.Delete(path);

        _thumbPaths.Clear();
    }

    private async Task<string> CreateThumbAsync(string thumbFileName, int length)
    {
        var thumbFilePath = await _thumbService.GetThumbLocalPathByFileNameAsync(thumbFileName);
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(thumbFilePath)!);
        await File.WriteAllBytesAsync(thumbFilePath, new byte[length]);
        _thumbPaths.Add(thumbFilePath);

        return thumbFilePath;
    }

    [Test]
    public async Task DeletePictureThumbsAsync_DeletesThumbsOfThePicture()
    {
        var thumb = await CreateThumbAsync("9999991_test-picture_100.jpeg", 1);
        var otherThumb = await CreateThumbAsync("9999992_test-picture_100.jpeg", 1);

        await _thumbService.DeletePictureThumbsAsync(new Picture { Id = 9999991 });

        File.Exists(thumb).Should().BeFalse();
        File.Exists(otherThumb).Should().BeTrue();
    }

    [Test]
    public async Task GetThumbsInfoAsync_ReturnsRealSizeOfThumbs()
    {
        var (filesCountBefore, filesSizeBefore) = await _thumbService.GetThumbsInfoAsync();

        await CreateThumbAsync("9999993_test-picture_100.jpeg", 5);
        var (filesCount, filesSize) = await _thumbService.GetThumbsInfoAsync();

        (filesCount - filesCountBefore).Should().Be(1);
        (filesSize - filesSizeBefore).Should().Be(5);
    }
}

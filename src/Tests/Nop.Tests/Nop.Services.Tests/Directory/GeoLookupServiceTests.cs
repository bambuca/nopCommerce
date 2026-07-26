using FluentAssertions;
using MaxMind.GeoIP2;
using Moq;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Directory;
using Nop.Services.Logging;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Directory;

/// <summary>
/// Verifies that <see cref="GeoLookupService"/> reuses a single underlying
/// <see cref="DatabaseReader"/> across calls. Without this guarantee the MaxMind reader
/// (which holds a memory-mapped file handle) is instantiated per call; under load the
/// finalizer cannot close the handles fast enough and the process exhausts its file
/// descriptor limit (<c>IOException: No file descriptors available</c>).
/// </summary>
[TestFixture]
public class GeoLookupServiceTests : BaseNopTest
{
    /// <summary>
    /// Test subclass that counts how many times the underlying reader is created.
    /// </summary>
    private sealed class CountingGeoLookupService : GeoLookupService
    {
        public int CreateReaderCallCount { get; private set; }

        public CountingGeoLookupService(ILogger logger, INopFileProvider fileProvider)
            : base(logger, fileProvider)
        {
        }

        protected override DatabaseReader CreateReader()
        {
            CreateReaderCallCount++;
            return base.CreateReader();
        }
    }

    private static CountingGeoLookupService CreateService()
    {
        var logger = new Mock<ILogger>().Object;
        return new CountingGeoLookupService(logger, CommonHelper.DefaultFileProvider);
    }

    [Test]
    public void DatabaseReaderIsLazilyCreatedOnFirstCall()
    {
        using var service = CreateService();

        service.CreateReaderCallCount.Should().Be(0);

        _ = service.LookupCountryIsoCode("8.8.8.8");

        service.CreateReaderCallCount.Should().Be(1);
    }

    [Test]
    public void DatabaseReaderIsReusedAcrossManyCalls()
    {
        using var service = CreateService();

        for (var i = 0; i < 100; i++)
            _ = service.LookupCountryIsoCode("8.8.8.8");

        service.CreateReaderCallCount.Should().Be(1);
    }

    [Test]
    public void DatabaseReaderIsReusedAcrossDifferentLookupMethods()
    {
        using var service = CreateService();

        _ = service.LookupCountryIsoCode("8.8.8.8");
        _ = service.LookupCountryName("8.8.8.8");
        _ = service.LookupCountryIsoCode("1.1.1.1");

        service.CreateReaderCallCount.Should().Be(1);
    }

    [Test]
    public void DisposeIsIdempotent()
    {
        var service = CreateService();
        _ = service.LookupCountryIsoCode("8.8.8.8");

        var act = () =>
        {
            service.Dispose();
            service.Dispose();
        };

        act.Should().NotThrow();
    }

    [Test]
    public void DisposeBeforeAnyCallDoesNotInstantiateReader()
    {
        var service = CreateService();

        service.Dispose();

        service.CreateReaderCallCount.Should().Be(0);
    }
}

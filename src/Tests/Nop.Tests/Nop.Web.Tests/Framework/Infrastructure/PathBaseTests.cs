using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Moq;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.Infrastructure;
using NUnit.Framework;
using NopApplicationBuilderExtensions = Nop.Web.Framework.Infrastructure.Extensions.ApplicationBuilderExtensions;

namespace Nop.Tests.Nop.Web.Tests.Framework.Infrastructure;

/// <summary>
/// Verifies how the application path base is applied: the middleware is added only when the application
/// owns the value, and the "X-Forwarded-Prefix" header is accepted only from a verifiable source.
/// </summary>
[TestFixture]
public class PathBaseTests
{
    #region Fields

    private AppSettings _originalAppSettings;

    #endregion

    #region Setup/Teardown

    [SetUp]
    public void SetUp()
    {
        _originalAppSettings = Singleton<AppSettings>.Instance;
    }

    [TearDown]
    public void TearDown()
    {
        Singleton<AppSettings>.Instance = _originalAppSettings;
    }

    #endregion

    #region Utilities

    private static HostingConfig CreateConfig(params (string Key, string Value)[] values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string>(value.Key, value.Value)))
            .Build();

        var hostingConfig = new HostingConfig();
        configuration.GetSection(nameof(HostingConfig))
            .Bind(hostingConfig, options => options.BindNonPublicProperties = true);

        return hostingConfig;
    }

    private static Mock<IApplicationBuilder> Configure(params (string Key, string Value)[] values)
    {
        Singleton<AppSettings>.Instance = new AppSettings(new List<IConfig> { CreateConfig(values) });

        var application = new Mock<IApplicationBuilder>();
        new NopPathBaseStartup().Configure(application.Object);

        return application;
    }

    private static void ShouldAddMiddleware(Mock<IApplicationBuilder> application, Times times, string because = "")
    {
        application.Verify(builder => builder.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), times, because);
    }

    private static bool AcceptsForwardedPrefix(params (string Key, string Value)[] values)
    {
        return NopApplicationBuilderExtensions.BuildForwardedHeadersOptions(CreateConfig(values))
            .ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedPrefix);
    }

    #endregion

    #region Tests

    [Test]
    public void PathBaseConfiguredAddsTheMiddleware()
    {
        ShouldAddMiddleware(Configure(("HostingConfig:PathBase", "/shop")), Times.Once());
    }

    [Test]
    public void NoPathBaseLeavesThePipelineUntouched()
    {
        //the default "/" means the application is hosted at the root
        ShouldAddMiddleware(Configure(("HostingConfig:PathBase", "/")), Times.Never());
    }

    [Test]
    public void ForwardedPrefixSkipsTheStaticPathBase()
    {
        ShouldAddMiddleware(Configure(
                ("HostingConfig:PathBase", "/shop"),
                ("HostingConfig:UseProxy", "true"),
                ("HostingConfig:UseForwardedPrefix", "true")),
            Times.Never(), "the proxy owns the value, applying a fixed one would prefix the path twice");
    }

    [Test]
    public void ForwardedPrefixWithoutProxyFallsBackToTheStaticPathBase()
    {
        //the forwarded headers middleware is only added when the proxy support is on; without it nothing
        //else would set the path base
        ShouldAddMiddleware(Configure(
            ("HostingConfig:PathBase", "/shop"),
            ("HostingConfig:UseProxy", "false"),
            ("HostingConfig:UseForwardedPrefix", "true")), Times.Once());
    }

    [Test]
    public void ForwardedPrefixIsNotAcceptedByDefault()
    {
        AcceptsForwardedPrefix(("HostingConfig:KnownNetworks", "10.0.0.0/8")).Should().BeFalse();
    }

    [Test]
    public void ForwardedPrefixIsAcceptedWithAKnownNetwork()
    {
        AcceptsForwardedPrefix(
            ("HostingConfig:UseForwardedPrefix", "true"),
            ("HostingConfig:KnownNetworks", "10.0.0.0/8")).Should().BeTrue();
    }

    [Test]
    public void ForwardedPrefixIsAcceptedWithAKnownProxy()
    {
        AcceptsForwardedPrefix(
            ("HostingConfig:UseForwardedPrefix", "true"),
            ("HostingConfig:KnownProxies", "10.0.0.1")).Should().BeTrue();
    }

    [Test]
    public void ForwardedPrefixIsDroppedWithoutAVerifiableSource()
    {
        AcceptsForwardedPrefix(("HostingConfig:UseForwardedPrefix", "true"))
            .Should().BeFalse("the middleware checks the remote address only while known proxies are configured");
    }

    [Test]
    public void ForwardedPrefixIsDroppedWhenTheKnownProxyCannotBeParsed()
    {
        AcceptsForwardedPrefix(
            ("HostingConfig:UseForwardedPrefix", "true"),
            ("HostingConfig:KnownProxies", "not-an-address")).Should().BeFalse();
    }

    [Test]
    public void ForwardedForAndProtoAreAlwaysAccepted()
    {
        var options = NopApplicationBuilderExtensions.BuildForwardedHeadersOptions(CreateConfig());

        options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedFor).Should().BeTrue();
        options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedProto).Should().BeTrue();
    }

    #endregion
}

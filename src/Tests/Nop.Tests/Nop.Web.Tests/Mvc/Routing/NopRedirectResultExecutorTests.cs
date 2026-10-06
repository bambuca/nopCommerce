using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nop.Core.Domain.Security;
using Nop.Services.Helpers;
using Nop.Web.Framework.Mvc.Routing;
using NUnit.Framework;

namespace Nop.Tests.Nop.Web.Tests.Mvc.Routing;

[TestFixture]
public class NopRedirectResultExecutorTests
{
    [Test]
    public async Task ShouldKeepEncodedQuerySeparatorsWhenRedirecting()
    {
        var location = await RedirectAsync("/search?q=a%26b%20%23c&adv=true");

        location.Should().Be("/search?q=a%26b%20%23c&adv=true");
    }

    [Test]
    public async Task ShouldKeepEncodedQuerySeparatorsWhenRedirectingToAbsoluteUrl()
    {
        var location = await RedirectAsync("https://www.example.com/search?q=a%26b%20%23c");

        location.Should().Be("https://www.example.com/search?q=a%26b%20%23c");
    }

    [Test]
    public async Task ShouldEncodeNonAsciiCharactersWhenRedirecting()
    {
        var location = await RedirectAsync("/search?q=müller");

        location.Should().Be("/search?q=m%C3%BCller");
    }

    [Test]
    public async Task ShouldNotEncodeAlreadyEncodedCharactersTwiceWhenRedirecting()
    {
        var location = await RedirectAsync("/search?q=m%C3%BCller");

        location.Should().Be("/search?q=m%C3%BCller");
    }

    [Test]
    public async Task ShouldKeepEncodedSlashesInPathOnSameHostWhenRedirecting()
    {
        var location = await RedirectAsync("/%2F%2Fwww.example.com");

        location.Should().Be("/%2F%2Fwww.example.com");
    }

    [Test]
    public async Task ShouldKeepEncodedQuerySeparatorsWhenRedirectingLocally()
    {
        var location = await LocalRedirectAsync("/search?q=a%26b%20%23c&adv=true");

        location.Should().Be("/search?q=a%26b%20%23c&adv=true");
    }

    [Test]
    public async Task ShouldEncodeNonAsciiCharactersWhenRedirectingLocally()
    {
        var location = await LocalRedirectAsync("/search?q=müller");

        location.Should().Be("/search?q=m%C3%BCller");
    }

    private static async Task<string> RedirectAsync(string url)
    {
        var context = CreateActionContext();
        var executor = new NopRedirectResultExecutor(NullLoggerFactory.Instance, Mock.Of<IUrlHelperFactory>(),
            CreateWebHelper(), Mock.Of<LinkGenerator>(), CreateSecuritySettings());

        await executor.ExecuteAsync(context, new RedirectResult(url) { UrlHelper = new UrlHelper(context) });

        return context.HttpContext.Response.Headers.Location.ToString();
    }

    private static async Task<string> LocalRedirectAsync(string url)
    {
        var context = CreateActionContext();
        var executor = new NopLocalRedirectResultExecutor(NullLoggerFactory.Instance, Mock.Of<IUrlHelperFactory>(),
            CreateWebHelper(), Mock.Of<LinkGenerator>(), CreateSecuritySettings());

        await executor.ExecuteAsync(context, new LocalRedirectResult(url) { UrlHelper = new UrlHelper(context) });

        return context.HttpContext.Response.Headers.Location.ToString();
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
    }

    private static IWebHelper CreateWebHelper()
    {
        var webHelper = new Mock<IWebHelper>();
        webHelper.Setup(helper => helper.CheckIsLocalUrl(It.IsAny<string>()))
            .Returns<string>(url => !string.IsNullOrEmpty(url) && RedirectHttpResult.IsLocalUrl(url));
        webHelper.Setup(helper => helper.GetStoreLocation(It.IsAny<bool?>()))
            .Returns($"http://{NopTestsDefaults.HostIpAddress}/");

        return webHelper.Object;
    }

    private static SecuritySettings CreateSecuritySettings()
    {
        return new SecuritySettings { AllowNonAsciiCharactersInHeaders = true };
    }
}
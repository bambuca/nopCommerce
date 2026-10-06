using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Localization;
using Nop.Services.Localization;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Localization;

[TestFixture]
public class LocalizedUrlExtensionsTests
{
    private static readonly PathString _pathBase = new("/shop");

    [Test]
    public void ShouldKeepEncodedQueryWhenRemovingApplicationPath()
    {
        "/shop/search?q=a%26b%20%23c&adv=true".RemoveApplicationPathFromRawUrl(_pathBase)
            .Should().Be("/search?q=a%26b%20%23c&adv=true");
    }

    [Test]
    public void ShouldDecodePathWhenRemovingApplicationPath()
    {
        "/shop/m%C3%BCller?q=m%C3%BCller".RemoveApplicationPathFromRawUrl(_pathBase)
            .Should().Be("/müller?q=m%C3%BCller");
        "/shop/m%C3%BCller".RemoveApplicationPathFromRawUrl(_pathBase)
            .Should().Be("/müller");
    }

    [Test]
    public void ShouldKeepEncodedQueryWhenAddingLanguageSeoCode()
    {
        var language = new Language { UniqueSeoCode = "en" };

        "/shop/search?q=a%26b%20%23c".AddLanguageSeoCodeToUrl(_pathBase, true, language)
            .Should().Be("/shop/en/search?q=a%26b%20%23c");
    }

    [Test]
    public void ShouldKeepEncodedQueryWhenReplacingLanguageSeoCode()
    {
        var language = new Language { UniqueSeoCode = "de" };

        "/shop/en/search?q=a%26b%20%23c".RemoveLanguageSeoCodeFromUrl(_pathBase, true)
            .AddLanguageSeoCodeToUrl(_pathBase, true, language)
            .Should().Be("/shop/de/search?q=a%26b%20%23c");
    }
}
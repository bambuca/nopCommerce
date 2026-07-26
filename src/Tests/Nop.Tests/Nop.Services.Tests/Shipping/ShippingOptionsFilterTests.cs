using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Configuration;
using Nop.Services.Shipping;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Shipping;

/// <summary>
/// Verifies the <see cref="IShippingOptionsFilter"/> seam: registered filters run at the end of
/// GetShippingOptionsAsync ordered by Order, rates they adjust are re-rounded by the caller according
/// to RoundPricesDuringCalculation, and with no filters registered the workflow is unchanged.
/// </summary>
[TestFixture]
public class ShippingOptionsFilterTests : ServiceTest
{
    #region Fields

    private const string FIXED_RATE_PROVIDER = "FixedRateTestShippingRateComputationMethod";

    private IList<ShoppingCartItem> _cart;
    private Address _address;

    #endregion

    #region Setup/Teardown

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        //activate the test shipping provider; the setting is read by ShippingService when it is resolved
        var settingService = GetService<ISettingService>();
        var shippingSettings = GetService<ShippingSettings>();
        if (!shippingSettings.ActiveShippingRateComputationMethodSystemNames.Contains(FIXED_RATE_PROVIDER))
            shippingSettings.ActiveShippingRateComputationMethodSystemNames.Add(FIXED_RATE_PROVIDER);
        await settingService.SaveSettingAsync(shippingSettings);

        //a ship-enabled product is required, otherwise no shipping package is created and the provider
        //is never called (the options come back empty without an error)
        var product = GetService<IRepository<Product>>().Table
            .First(candidate => candidate.IsShipEnabled && candidate.Published && !candidate.Deleted);
        _cart = new List<ShoppingCartItem> { new() { ProductId = product.Id, Quantity = 1 } };
        _address = new Address { CountryId = 1 };
    }

    [TearDown]
    public void TearDown()
    {
        TestShippingOptionsFilterAlpha.Reset();
        TestShippingOptionsFilterBeta.Reset();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        //restore the global settings so that other fixtures do not inherit the activated test provider
        var settingService = GetService<ISettingService>();
        var shippingSettings = GetService<ShippingSettings>();
        shippingSettings.ActiveShippingRateComputationMethodSystemNames.Remove(FIXED_RATE_PROVIDER);
        await settingService.SaveSettingAsync(shippingSettings);
    }

    #endregion

    #region Utilities

    private static IShippingService ResolveShippingService()
    {
        //resolve from a fresh scope, settings snapshots are bound per scope
        var scope = GetService<IServiceScopeFactory>().CreateScope();
        return GetService<IShippingService>(scope);
    }

    #endregion

    #region Tests

    [Test]
    public async Task WithoutFiltersTheWorkflowIsUnchanged()
    {
        var response = await ResolveShippingService()
            .GetShippingOptionsAsync(_cart, _address,
                allowedShippingRateComputationMethodSystemName: FIXED_RATE_PROVIDER);

        response.Errors.Should().BeEmpty();
        //the fixed rate test provider returns two options
        response.ShippingOptions.Should().HaveCount(2);
    }

    [Test]
    public async Task FilterCanRemoveOptions()
    {
        TestShippingOptionsFilterAlpha.Handler = response =>
        {
            response.ShippingOptions.Clear();
            return Task.CompletedTask;
        };

        var response = await ResolveShippingService()
            .GetShippingOptionsAsync(_cart, _address,
                allowedShippingRateComputationMethodSystemName: FIXED_RATE_PROVIDER);

        response.ShippingOptions.Should().BeEmpty();
    }

    [Test]
    public async Task FiltersRunOrderedByOrder()
    {
        var calls = new List<string>();
        TestShippingOptionsFilterAlpha.OrderValue = 20;
        TestShippingOptionsFilterAlpha.Handler = _ =>
        {
            calls.Add("alpha");
            return Task.CompletedTask;
        };
        TestShippingOptionsFilterBeta.OrderValue = 10;
        TestShippingOptionsFilterBeta.Handler = _ =>
        {
            calls.Add("beta");
            return Task.CompletedTask;
        };

        await ResolveShippingService()
            .GetShippingOptionsAsync(_cart, _address,
                allowedShippingRateComputationMethodSystemName: FIXED_RATE_PROVIDER);

        calls.Should().Equal("beta", "alpha");
    }

    [Test]
    public async Task AdjustedRatesAreRoundedByTheCaller()
    {
        TestShippingOptionsFilterAlpha.Handler = response =>
        {
            foreach (var option in response.ShippingOptions)
                option.Rate = 9.9949m;
            return Task.CompletedTask;
        };

        var response = await ResolveShippingService()
            .GetShippingOptionsAsync(_cart, _address,
                allowedShippingRateComputationMethodSystemName: FIXED_RATE_PROVIDER);

        //the filter does not round; the contract is that the caller re-applies the rounding policy
        response.ShippingOptions.Should().OnlyContain(option => option.Rate == 9.99m);
    }

    #endregion
}

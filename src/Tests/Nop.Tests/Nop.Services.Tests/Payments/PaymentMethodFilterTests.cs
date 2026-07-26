using FluentAssertions;
using Moq;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Payments;

/// <summary>
/// Verifies the <see cref="IPaymentMethodFilter"/> seam: a filter shapes the listing returned by
/// LoadActivePluginsAsync and adjusts the fee returned by GetAdditionalHandlingFeeAsync, whose result
/// stays clamped to a non-negative value. With no filters registered the workflow is unchanged.
/// </summary>
/// <remarks>
/// The manager and the service are composed by hand over a mocked <see cref="IPluginService"/> exposing
/// a single payment method, so the fixture does not depend on the plugins installed in the test harness.
/// The seam itself resolves through the real test container, where the filter double is registered.
/// </remarks>
[TestFixture]
public class PaymentMethodFilterTests : ServiceTest
{
    #region Fields

    private const string TEST_METHOD = "Payments.TestMethod";

    private PaymentPluginManager _paymentPluginManager;
    private PaymentService _paymentService;

    #endregion

    #region Setup/Teardown

    [SetUp]
    public void SetUp()
    {
        var descriptor = new PluginDescriptor
        {
            PluginType = typeof(TestPaymentMethod),
            SystemName = TEST_METHOD,
            Installed = true,
            ReferencedAssembly = typeof(TestPaymentMethod).Assembly
        };
        var testMethod = new TestPaymentMethod { PluginDescriptor = descriptor };

        var pluginService = new Mock<IPluginService>();
        pluginService.Setup(service => service.GetPluginsAsync<IPaymentMethod>(
                It.IsAny<LoadPluginsMode>(), It.IsAny<Customer>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new List<IPaymentMethod> { testMethod });
        pluginService.Setup(service => service.GetPluginDescriptorBySystemNameAsync<IPaymentMethod>(
                TEST_METHOD, It.IsAny<LoadPluginsMode>(), It.IsAny<Customer>(), It.IsAny<int>(),
                It.IsAny<string>()))
            .ReturnsAsync(descriptor);

        var paymentSettings = new PaymentSettings { ActivePaymentMethodSystemNames = new List<string> { TEST_METHOD } };

        _paymentPluginManager = new PaymentPluginManager(GetService<ICustomerService>(),
            pluginService.Object, GetService<ISettingService>(), paymentSettings);
        _paymentService = new PaymentService(GetService<ICustomerService>(), _paymentPluginManager,
            GetService<IPriceCalculationService>(), paymentSettings,
            new ShoppingCartSettings { RoundPricesDuringCalculation = false });
    }

    [TearDown]
    public void TearDown()
    {
        TestPaymentMethodFilter.Reset();
    }

    #endregion

    #region Tests

    [Test]
    public async Task WithoutFiltersTheWorkflowIsUnchanged()
    {
        var methods = await _paymentPluginManager.LoadActivePluginsAsync();

        methods.Select(method => method.PluginDescriptor.SystemName).Should().Contain(TEST_METHOD);
    }

    [Test]
    public async Task FilterAdjustsAdditionalHandlingFee()
    {
        TestPaymentMethodFilter.FeeHandler = fee => fee + 5m;

        var result = await _paymentService
            .GetAdditionalHandlingFeeAsync(new List<ShoppingCartItem>(), TEST_METHOD);

        result.Should().Be(5m, "the test method returns a zero fee and the filter adds 5");
    }

    [Test]
    public async Task FilterShapesActiveMethodListing()
    {
        TestPaymentMethodFilter.MethodsHandler = methods => methods
            .Where(method => method.PluginDescriptor.SystemName != TEST_METHOD).ToList();

        var methods = await _paymentPluginManager.LoadActivePluginsAsync();

        methods.Should().BeEmpty();
    }

    [Test]
    public async Task NegativeFeeAfterFilterIsClampedToZero()
    {
        TestPaymentMethodFilter.FeeHandler = _ => -10m;

        var result = await _paymentService
            .GetAdditionalHandlingFeeAsync(new List<ShoppingCartItem>(), TEST_METHOD);

        result.Should().Be(decimal.Zero);
    }

    #endregion
}

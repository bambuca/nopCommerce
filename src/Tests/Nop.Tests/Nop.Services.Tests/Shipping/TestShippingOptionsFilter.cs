using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Shipping;

namespace Nop.Tests.Nop.Services.Tests.Shipping;

/// <summary>
///     Configurable test doubles for the shipping options filter seam. Both are registered in the
///     test container (BaseNopTest); with null handlers they are no-ops, so unrelated tests are
///     unaffected. Two classes exist to exercise the Order-based sequencing.
/// </summary>
public class TestShippingOptionsFilterAlpha : IShippingOptionsFilter
{
    #region Methods

    public static void Reset()
    {
        OrderValue = 100;
        Handler = null;
    }

    public Task AdjustShippingOptionsAsync(GetShippingOptionResponse response, IList<ShoppingCartItem> cart,
        Address shippingAddress, Customer customer, int storeId)
    {
        return Handler?.Invoke(response) ?? Task.CompletedTask;
    }

    #endregion

    #region Properties

    public static int OrderValue { get; set; } = 100;

    public static Func<GetShippingOptionResponse, Task> Handler { get; set; }

    public int Order => OrderValue;

    #endregion
}

/// <summary>Second configurable seam test double (see <see cref="TestShippingOptionsFilterAlpha" />).</summary>
public class TestShippingOptionsFilterBeta : IShippingOptionsFilter
{
    #region Methods

    public static void Reset()
    {
        OrderValue = 200;
        Handler = null;
    }

    public Task AdjustShippingOptionsAsync(GetShippingOptionResponse response, IList<ShoppingCartItem> cart,
        Address shippingAddress, Customer customer, int storeId)
    {
        return Handler?.Invoke(response) ?? Task.CompletedTask;
    }

    #endregion

    #region Properties

    public static int OrderValue { get; set; } = 200;

    public static Func<GetShippingOptionResponse, Task> Handler { get; set; }

    public int Order => OrderValue;

    #endregion
}
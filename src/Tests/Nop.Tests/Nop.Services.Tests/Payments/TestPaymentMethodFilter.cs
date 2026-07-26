using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Payments;

namespace Nop.Tests.Nop.Services.Tests.Payments;

/// <summary>
///     Configurable test double for the payment method filter seam. Registered in the test container
///     (BaseNopTest); with null handlers it is a no-op, so unrelated tests are unaffected.
/// </summary>
public class TestPaymentMethodFilter : IPaymentMethodFilter
{
    #region Methods

    public static void Reset()
    {
        OrderValue = 100;
        MethodsHandler = null;
        FeeHandler = null;
    }

    public Task<IList<IPaymentMethod>> FilterPaymentMethodsAsync(IList<IPaymentMethod> paymentMethods,
        Customer customer, int storeId, int countryId)
    {
        return Task.FromResult(MethodsHandler?.Invoke(paymentMethods) ?? paymentMethods);
    }

    public Task<decimal> AdjustAdditionalHandlingFeeAsync(decimal fee, IList<ShoppingCartItem> cart,
        IPaymentMethod paymentMethod)
    {
        return Task.FromResult(FeeHandler?.Invoke(fee) ?? fee);
    }

    #endregion

    #region Properties

    public static int OrderValue { get; set; } = 100;

    public static Func<IList<IPaymentMethod>, IList<IPaymentMethod>> MethodsHandler { get; set; }

    public static Func<decimal, decimal> FeeHandler { get; set; }

    public int Order => OrderValue;

    #endregion
}
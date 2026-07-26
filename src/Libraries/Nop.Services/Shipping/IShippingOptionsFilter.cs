using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Shipping;

/// <summary>
/// Represents a filter applied to shipping options loaded from all shipping rate computation methods.
/// Implementations are resolved from DI and applied ordered by <see cref="Order"/> on every load
/// (checkout, estimate, recalculations alike). With no implementations registered the shipping
/// workflow is unchanged.
/// </summary>
public partial interface IShippingOptionsFilter
{
    /// <summary>
    /// Gets the order of the filter (filters with a lower value run first)
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Adjusts aggregated shipping options in place (may remove options, change their rates, or touch errors).
    /// Adjusted rates are re-rounded by the caller afterwards (per ShoppingCartSettings.RoundPricesDuringCalculation),
    /// so implementations do not need to round themselves.
    /// </summary>
    /// <param name="response">Aggregated response of all shipping rate computation methods</param>
    /// <param name="cart">Shopping cart</param>
    /// <param name="shippingAddress">Shipping address</param>
    /// <param name="customer">Customer; can be null</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task AdjustShippingOptionsAsync(GetShippingOptionResponse response, IList<ShoppingCartItem> cart,
        Address shippingAddress, Customer customer, int storeId);
}

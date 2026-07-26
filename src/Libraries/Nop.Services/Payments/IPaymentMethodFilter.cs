using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Payments;

/// <summary>
/// Represents a filter applied to active payment methods offered during checkout and to their additional handling fees.
/// Implementations are resolved from DI and applied ordered by <see cref="Order"/>. The filter shapes
/// the payment method listing (<see cref="IPaymentPluginManager.LoadActivePluginsAsync"/>) only — like the native
/// country restriction or <see cref="IPaymentMethod.HidePaymentMethodAsync"/>, it is NOT re-checked when a posted
/// selection is validated, nor applied to operations on already placed orders (capture, refund, void, recurring, admin).
/// With no implementations registered the payment workflow is unchanged.
/// </summary>
public partial interface IPaymentMethodFilter
{
    /// <summary>
    /// Gets the order of the filter (filters with a lower value run first)
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Filters active payment methods offered to the customer
    /// </summary>
    /// <param name="paymentMethods">Active payment methods (already filtered by store, ACL and country restrictions)</param>
    /// <param name="customer">Customer; can be null</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="countryId">Country identifier used by the native country restriction; 0 if not applied</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the filtered list of payment methods
    /// </returns>
    Task<IList<IPaymentMethod>> FilterPaymentMethodsAsync(IList<IPaymentMethod> paymentMethods,
        Customer customer, int storeId, int countryId);

    /// <summary>
    /// Adjusts the additional handling fee of a payment method
    /// </summary>
    /// <param name="fee">Fee returned by the payment method (or by a previous filter)</param>
    /// <param name="cart">Shopping cart</param>
    /// <param name="paymentMethod">Payment method the fee belongs to</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the adjusted fee
    /// </returns>
    Task<decimal> AdjustAdditionalHandlingFeeAsync(decimal fee, IList<ShoppingCartItem> cart,
        IPaymentMethod paymentMethod);
}

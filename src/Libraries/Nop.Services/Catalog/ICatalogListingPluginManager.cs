using Nop.Core.Domain.Customers;
using Nop.Services.Plugins;

namespace Nop.Services.Catalog;

/// <summary>
/// Provides an interface for catalog listing plugin manager
/// </summary>
public partial interface ICatalogListingPluginManager : IPluginManager<ICatalogListingProvider>
{
    /// <summary>
    /// Load primary active catalog listing provider
    /// </summary>
    /// <param name="customer">Filter by customer; pass null to load all plugins</param>
    /// <param name="storeId">Filter by store; pass 0 to load all plugins</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the catalog listing provider
    /// </returns>
    Task<ICatalogListingProvider> LoadPrimaryPluginAsync(Customer customer = null, int storeId = 0);

    /// <summary>
    /// Check whether the passed catalog listing provider is active
    /// </summary>
    /// <param name="catalogListingProvider">Catalog listing provider to check</param>
    /// <returns>Result</returns>
    bool IsPluginActive(ICatalogListingProvider catalogListingProvider);

    /// <summary>
    /// Check whether the catalog listing provider with the passed system name is active
    /// </summary>
    /// <param name="systemName">System name of catalog listing provider to check</param>
    /// <param name="customer">Filter by customer; pass null to load all plugins</param>
    /// <param name="storeId">Filter by store; pass 0 to load all plugins</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    Task<bool> IsPluginActiveAsync(string systemName, Customer customer = null, int storeId = 0);
}

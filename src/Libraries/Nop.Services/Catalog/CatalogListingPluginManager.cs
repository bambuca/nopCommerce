using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Services.Plugins;

namespace Nop.Services.Catalog;

/// <summary>
/// Represents a catalog listing plugin manager implementation
/// </summary>
public partial class CatalogListingPluginManager : PluginManager<ICatalogListingProvider>, ICatalogListingPluginManager
{
    #region Fields

    protected readonly CatalogSettings _catalogSettings;

    #endregion

    #region Ctor

    public CatalogListingPluginManager(CatalogSettings catalogSettings, ICustomerService customerService, IPluginService pluginService)
        : base(customerService, pluginService)
    {
        _catalogSettings = catalogSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Load primary active catalog listing provider
    /// </summary>
    /// <param name="customer">Filter by customer; pass null to load all plugins</param>
    /// <param name="storeId">Filter by store; pass 0 to load all plugins</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the catalog listing provider
    /// </returns>
    public virtual async Task<ICatalogListingProvider> LoadPrimaryPluginAsync(Customer customer = null, int storeId = 0)
    {
        if (string.IsNullOrEmpty(_catalogSettings.ActiveCatalogListingProviderSystemName))
            return null;

        return await LoadPrimaryPluginAsync(_catalogSettings.ActiveCatalogListingProviderSystemName, customer, storeId);
    }

    /// <summary>
    /// Check whether the passed catalog listing provider is active
    /// </summary>
    /// <param name="catalogListingProvider">Catalog listing provider to check</param>
    /// <returns>Result</returns>
    public virtual bool IsPluginActive(ICatalogListingProvider catalogListingProvider)
    {
        return IsPluginActive(catalogListingProvider, [_catalogSettings.ActiveCatalogListingProviderSystemName]);
    }

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
    public virtual async Task<bool> IsPluginActiveAsync(string systemName, Customer customer = null, int storeId = 0)
    {
        var catalogListingProvider = await LoadPluginBySystemNameAsync(systemName, customer, storeId);
        return IsPluginActive(catalogListingProvider);
    }

    #endregion
}

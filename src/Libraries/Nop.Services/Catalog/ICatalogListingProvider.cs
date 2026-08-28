using Nop.Services.Plugins;

namespace Nop.Services.Catalog;

/// <summary>
/// Provides an interface for creating a catalog listing provider
/// </summary>
/// <remarks>
/// A catalog listing is one question — which products are on this page, and what should the filters
/// beside them offer — that nopCommerce answers with several independent queries. A search engine
/// answers all of it in a single round trip, and only an engine that sees the products and the
/// filters together can tell how many products are behind each filter option. This interface lets
/// such an engine answer the whole listing at once, while
/// <see cref="ISearchProvider"/> stays what it is: the narrower hook that turns keywords into
/// product identifiers.
/// </remarks>
public partial interface ICatalogListingProvider : IPlugin
{
    /// <summary>
    /// Get the catalog listing for the specified request
    /// </summary>
    /// <param name="request">Request describing the listing to prepare</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the listing; any part of it left null is queried by nopCommerce itself
    /// </returns>
    Task<CatalogListingResult> GetListingAsync(CatalogListingRequest request);
}

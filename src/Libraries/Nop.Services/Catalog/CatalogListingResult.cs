using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

/// <summary>
/// Represents the answer of a catalog listing provider
/// </summary>
/// <remarks>
/// Every property is optional. A null property means the provider does not answer that part of the
/// listing and nopCommerce queries it on its own, exactly as it does when no provider is active.
/// A provider that only knows how to count the facets therefore leaves the products null, and one
/// that only knows how to find the products leaves the filters null.
/// </remarks>
public partial class CatalogListingResult
{
    /// <summary>
    /// Gets or sets the requested page of products
    /// </summary>
    public IPagedList<Product> Products { get; set; }

    /// <summary>
    /// Gets or sets the price range available within the listing. Unlike the range nopCommerce
    /// computes on its own, a provider is free to narrow it down to the products that pass the
    /// filters the customer has already selected
    /// </summary>
    public CatalogPriceRange AvailablePriceRange { get; set; }

    /// <summary>
    /// Gets or sets the specification attribute options to offer as filters. A provider is free to
    /// leave out the options that no product in the listing has, which nopCommerce cannot do on its own
    /// </summary>
    public IList<SpecificationAttributeOption> FilterableSpecificationOptions { get; set; }

    /// <summary>
    /// Gets or sets the manufacturers to offer as filters
    /// </summary>
    public IList<Manufacturer> FilterableManufacturers { get; set; }

    /// <summary>
    /// Gets or sets the number of products behind each specification attribute option, keyed by the
    /// option identifier. Options missing from the dictionary are rendered without a count
    /// </summary>
    public IDictionary<int, int> SpecificationOptionProductCounts { get; set; }

    /// <summary>
    /// Gets or sets the number of products behind each manufacturer, keyed by the manufacturer
    /// identifier. Manufacturers missing from the dictionary are rendered without a count
    /// </summary>
    public IDictionary<int, int> ManufacturerProductCounts { get; set; }
}

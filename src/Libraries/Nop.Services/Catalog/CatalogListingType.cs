namespace Nop.Services.Catalog;

/// <summary>
/// Represents a type of the catalog listing
/// </summary>
public enum CatalogListingType
{
    /// <summary>
    /// Products of a category
    /// </summary>
    Category = 10,

    /// <summary>
    /// Products of a manufacturer
    /// </summary>
    Manufacturer = 20,

    /// <summary>
    /// Products of a vendor
    /// </summary>
    Vendor = 30,

    /// <summary>
    /// Products of a product tag
    /// </summary>
    ProductTag = 40,

    /// <summary>
    /// Products matching a search request
    /// </summary>
    Search = 50
}

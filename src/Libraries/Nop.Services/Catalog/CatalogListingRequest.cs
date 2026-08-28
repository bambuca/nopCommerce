using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

/// <summary>
/// Represents a request for a catalog listing, i.e. a page of products together with everything
/// needed to render the filters shown next to them
/// </summary>
public partial class CatalogListingRequest
{
    /// <summary>
    /// Gets or sets the type of the listing being prepared
    /// </summary>
    public CatalogListingType ListingType { get; set; }

    /// <summary>
    /// Gets or sets the category identifiers to load the products from; includes the subcategories
    /// when the corresponding setting is enabled
    /// </summary>
    public IList<int> CategoryIds { get; set; }

    /// <summary>
    /// Gets or sets the manufacturer identifiers to load the products from; for a category listing
    /// these are the manufacturers selected by the customer in the filter
    /// </summary>
    public IList<int> ManufacturerIds { get; set; }

    /// <summary>
    /// Gets or sets the vendor identifier to load the products from; 0 to load from all vendors
    /// </summary>
    public int VendorId { get; set; }

    /// <summary>
    /// Gets or sets the product tag identifier to load the products from; 0 to ignore the tags
    /// </summary>
    public int ProductTagId { get; set; }

    /// <summary>
    /// Gets or sets the search keywords; null or empty for a listing that is not a search
    /// </summary>
    public string Keywords { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to search in the product descriptions
    /// </summary>
    public bool SearchInDescriptions { get; set; }

    /// <summary>
    /// Gets or sets the store identifier
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the working language
    /// </summary>
    public int LanguageId { get; set; }

    /// <summary>
    /// Gets or sets the identifiers of the specification attribute options selected by the customer
    /// </summary>
    public IList<int> SelectedSpecificationOptionIds { get; set; }

    /// <summary>
    /// Gets or sets the lowest price selected by the customer
    /// </summary>
    public decimal? PriceMin { get; set; }

    /// <summary>
    /// Gets or sets the highest price selected by the customer
    /// </summary>
    public decimal? PriceMax { get; set; }

    /// <summary>
    /// Gets or sets the order to sort the products by
    /// </summary>
    public ProductSortingEnum OrderBy { get; set; }

    /// <summary>
    /// Gets or sets the page index; zero-based
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to load only the products marked as visible individually
    /// </summary>
    public bool VisibleIndividuallyOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to exclude the featured products
    /// </summary>
    public bool ExcludeFeaturedProducts { get; set; }
}

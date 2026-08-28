namespace Nop.Services.Catalog;

/// <summary>
/// Represents a price range available within a catalog listing
/// </summary>
/// <param name="From">The lowest price, or null if it is unknown</param>
/// <param name="To">The highest price, or null if it is unknown</param>
public partial record CatalogPriceRange(decimal? From, decimal? To);

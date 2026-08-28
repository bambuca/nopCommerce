using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Catalog;

/// <summary>
/// Represents a manufacturer filter model
/// </summary>
public partial record ManufacturerFilterModel : BaseNopModel
{
    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether filtering is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the filtrable manufacturers
    /// </summary>
    public IList<SelectListItem> Manufacturers { get; set; }

    /// <summary>
    /// Gets or sets the number of products behind each manufacturer, keyed by the manufacturer
    /// identifier. Empty unless a catalog listing provider counts the facets; manufacturers missing
    /// from it are rendered without a count
    /// </summary>
    /// <remarks>
    /// A dictionary beside the list rather than a property on the item, because the items are
    /// <see cref="SelectListItem"/> and changing that type would break the existing views
    /// </remarks>
    public IDictionary<int, int> ProductCounts { get; set; }

    #endregion

    #region Ctor

    public ManufacturerFilterModel()
    {
        Manufacturers = new List<SelectListItem>();
        ProductCounts = new Dictionary<int, int>();
    }

    #endregion
}
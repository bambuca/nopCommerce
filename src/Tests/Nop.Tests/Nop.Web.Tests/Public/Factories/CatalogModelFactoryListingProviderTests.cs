using System.Globalization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Newtonsoft.Json;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Vendors;
using Nop.Core.Events;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.FilterLevels;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Vendors;
using Nop.Web.Factories;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;

namespace Nop.Tests.Nop.Web.Tests.Public.Factories;

/// <summary>
/// Verifies that a catalog listing provider is allowed to answer a listing, that each part of its
/// answer is honoured on its own, and that anything it leaves out still falls back to the query
/// nopCommerce runs when no provider is active
/// </summary>
[TestFixture]
public class CatalogModelFactoryListingProviderTests : WebTest
{
    private Category _category;
    private CatalogSettings _catalogSettings;
    private TestableCatalogModelFactory _catalogModelFactory;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        //the same category the rest of the catalog factory tests use, because the sample data puts
        //products, manufacturers and filterable options on it
        _category = (await GetService<ICategoryService>().GetAllCategoriesAsync("Notebooks")).First();
        _catalogSettings = GetService<CatalogSettings>();
        _catalogModelFactory = ActivatorUtilities.CreateInstance<TestableCatalogModelFactory>(ServiceProvider);
    }

    [SetUp]
    public void ClearProvidedListing()
    {
        _catalogModelFactory.ProvidedListing = null;
    }

    [Test]
    public async Task WithoutProviderProductsComeFromTheStandardQuery()
    {
        var model = await _catalogModelFactory.PrepareCategoryProductsModelAsync(_category,
            new CatalogProductsCommand { PageNumber = 1, PageSize = 10 });

        model.Products.Should().NotBeEmpty();
    }

    [Test]
    public async Task ProviderProductsAreUsedInsteadOfTheStandardQuery()
    {
        var product = await GetService<IProductService>().GetProductByIdAsync(1);
        _catalogModelFactory.ProvidedListing = new CatalogListingResult
        {
            Products = new PagedList<Product>(new List<Product> { product }, 0, 10, 1)
        };

        var model = await _catalogModelFactory.PrepareCategoryProductsModelAsync(_category,
            new CatalogProductsCommand { PageNumber = 1, PageSize = 10 });

        model.Products.Should().ContainSingle().Which.Id.Should().Be(product.Id);
    }

    [Test]
    public async Task ProviderIsToldWhichListingItIsAskedAbout()
    {
        var command = new CatalogProductsCommand { PageNumber = 2, PageSize = 7 };

        await _catalogModelFactory.PrepareCategoryProductsModelAsync(_category, command);

        var request = _catalogModelFactory.LastRequest;
        request.Should().NotBeNull();
        request.ListingType.Should().Be(CatalogListingType.Category);
        request.CategoryIds.Should().Contain(_category.Id);
        request.PageIndex.Should().Be(command.PageNumber - 1);

        //the page size the provider is asked for is the one nopCommerce settled on, not the one
        //that arrived from the query string
        request.PageSize.Should().Be(command.PageSize);
    }

    [Test]
    public async Task ProviderFacetCountsReachTheModel()
    {
        var filteringWasEnabled = _catalogSettings.EnableSpecificationAttributeFiltering;
        _catalogSettings.EnableSpecificationAttributeFiltering = true;

        try
        {
            //the options a provider offers are its own choice, so any existing one will do here
            var option = await GetService<ISpecificationAttributeService>()
                .GetSpecificationAttributeOptionByIdAsync(1);

            _catalogModelFactory.ProvidedListing = new CatalogListingResult
            {
                FilterableSpecificationOptions = new List<SpecificationAttributeOption> { option },
                SpecificationOptionProductCounts = new Dictionary<int, int> { [option.Id] = 42 }
            };

            var model = await _catalogModelFactory.PrepareCategoryProductsModelAsync(_category,
                new CatalogProductsCommand { PageNumber = 1, PageSize = 10 });

            model.SpecificationFilter.Attributes
                .SelectMany(attribute => attribute.Values)
                .Single(value => value.Id == option.Id)
                .ProductCount.Should().Be(42);
        }
        finally
        {
            _catalogSettings.EnableSpecificationAttributeFiltering = filteringWasEnabled;
        }
    }

    /// <summary>
    /// Stands in for an active catalog listing provider. The plugin infrastructure is not involved,
    /// so the test is about the factory honouring the answer, not about how a provider is loaded
    /// </summary>
    private sealed class TestableCatalogModelFactory : CatalogModelFactory
    {
        public CatalogListingResult ProvidedListing { get; set; }

        public CatalogListingRequest LastRequest { get; private set; }

        public TestableCatalogModelFactory(CatalogSettings catalogSettings,
            CustomerSettings customerSettings,
            ForumSettings forumSettings,
            ICatalogListingPluginManager catalogListingPluginManager,
            ICategoryService categoryService,
            ICategoryTemplateService categoryTemplateService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IEventPublisher eventPublisher,
            IFilterLevelValueService filterLevelValueService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            IJsonLdModelFactory jsonLdModelFactory,
            ILocalizationService localizationService,
            IManufacturerService manufacturerService,
            IManufacturerTemplateService manufacturerTemplateService,
            INopUrlHelper nopUrlHelper,
            IPictureService pictureService,
            IProductModelFactory productModelFactory,
            IProductReviewService productReviewService,
            IProductService productService,
            IProductTagService productTagService,
            ISearchTermService searchTermService,
            ISpecificationAttributeService specificationAttributeService,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            IUrlRecordService urlRecordService,
            IVendorService vendorService,
            IWebHelper webHelper,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            SeoSettings seoSettings,
            VendorSettings vendorSettings)
            : base(catalogSettings,
                customerSettings,
                forumSettings,
                catalogListingPluginManager,
                categoryService,
                categoryTemplateService,
                currencyService,
                customerService,
                eventPublisher,
                filterLevelValueService,
                genericAttributeService,
                httpContextAccessor,
                jsonLdModelFactory,
                localizationService,
                manufacturerService,
                manufacturerTemplateService,
                nopUrlHelper,
                pictureService,
                productModelFactory,
                productReviewService,
                productService,
                productTagService,
                searchTermService,
                specificationAttributeService,
                staticCacheManager,
                storeContext,
                urlRecordService,
                vendorService,
                webHelper,
                workContext,
                mediaSettings,
                seoSettings,
                vendorSettings)
        {
        }

        protected override Task<CatalogListingResult> GetProvidedListingAsync(CatalogListingRequest request)
        {
            LastRequest = request;

            return Task.FromResult(ProvidedListing);
        }
    }
}

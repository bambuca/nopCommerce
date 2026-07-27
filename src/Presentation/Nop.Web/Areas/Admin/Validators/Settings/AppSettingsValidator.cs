using FluentValidation;
using Nop.Services.Localization;
using Nop.Web.Areas.Admin.Models.Settings;
using Nop.Web.Framework.Validators;

namespace Nop.Web.Areas.Admin.Validators.Settings;

/// <summary>
/// Represents a validator of the app settings model
/// </summary>
/// <remarks>
/// The rules target the nested configuration models on purpose: the action accepts <see cref="AppSettingsModel"/>,
/// and a validator declared for a nested model alone would never be invoked by the automatic validation.
/// The validated values take effect on the application start only, so an invalid one is not reported by a
/// failing request but by an application that no longer boots.
/// </remarks>
public partial class AppSettingsValidator : BaseNopValidator<AppSettingsModel>
{
    #region Ctor

    public AppSettingsValidator(ILocalizationService localizationService)
    {
        //an empty value means "hosted at the root"; anything else has to be a rooted path, otherwise
        //the PathString the path base middleware is built from throws and the application fails to start
        RuleFor(model => model.HostingConfigModel.PathBase)
            .Must(pathBase => string.IsNullOrWhiteSpace(pathBase) ||
                              (pathBase.StartsWith('/') && pathBase.IndexOfAny(['?', '#', ' ']) < 0))
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Configuration.AppSettings.Hosting.PathBase.Invalid"));

        //the header carrying the path base is read by the forwarded headers middleware, which is only
        //added when the proxy support is on; without it nothing would set the path base at all
        RuleFor(model => model.HostingConfigModel)
            .Must(hosting => !hosting.UseForwardedPrefix || hosting.UseProxy)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Configuration.AppSettings.Hosting.UseForwardedPrefix.RequiresProxy"));

        //and the middleware verifies the source of the headers only while known proxies or networks are
        //configured - otherwise any caller could dictate the path base of the store
        RuleFor(model => model.HostingConfigModel)
            .Must(hosting => !hosting.UseForwardedPrefix ||
                             !string.IsNullOrWhiteSpace(hosting.KnownProxies) ||
                             !string.IsNullOrWhiteSpace(hosting.KnownNetworks))
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Configuration.AppSettings.Hosting.UseForwardedPrefix.RequiresKnownProxies"));
    }

    #endregion
}

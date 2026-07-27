using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Infrastructure;

/// <summary>
/// Represents object for the configuring of the application path base on application startup
/// </summary>
public partial class NopPathBaseStartup : INopStartup
{
    /// <summary>
    /// Add and configure any of the middleware
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    /// <summary>
    /// Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public void Configure(IApplicationBuilder application)
    {
        var hostingConfig = Singleton<AppSettings>.Instance.Get<HostingConfig>();

        //when the path base comes from the "X-Forwarded-Prefix" header, the proxy owns the value and
        //the forwarded headers middleware has already put it into Request.PathBase; applying a fixed one
        //on top of that would prefix the path twice. UseProxy has to be checked as well - without it the
        //middleware reading the header is not in the pipeline at all, and skipping here would leave the
        //application running with no path base whatsoever
        if (hostingConfig.UseForwardedPrefix && hostingConfig.UseProxy)
            return;

        var pathBase = new PathString(hostingConfig.PathBase?.TrimEnd('/'));

        if (!pathBase.HasValue)
            return;

        //using UsePathBaseMiddleware directly instead of app.UsePathBase() to prevent the reroute branch
        //created internally by RerouteHelper, so that all middlewares (static files, routing, ...) keep
        //running in the same pipeline
        application.Use(next => new UsePathBaseMiddleware(next, pathBase).Invoke);
    }

    /// <summary>
    /// Gets order of this startup configuration implementation
    /// </summary>
    public int Order => 80; //the path base has to be set before routing and static files
}

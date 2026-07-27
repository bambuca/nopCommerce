namespace Nop.Core.Configuration;

/// <summary>
/// Represents hosting configuration parameters
/// </summary>
public partial class HostingConfig : IConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether to use proxy servers and load balancers
    /// </summary>
    public bool UseProxy { get; protected set; }

    /// <summary>
    /// Gets or sets the header used to retrieve the value for the originating scheme (HTTP/HTTPS)
    /// </summary>
    public string ForwardedProtoHeaderName { get; protected set; } = string.Empty;

    /// <summary>
    /// Gets or sets the header used to retrieve the originating client IP
    /// </summary>
    public string ForwardedForHeaderName { get; protected set; } = string.Empty;

    /// <summary>
    /// Gets or sets addresses of known proxies to accept forwarded headers from
    /// </summary>
    public string KnownProxies { get; protected set; } = string.Empty;

    /// <summary>
    /// Gets or sets addresses of known networks to accept forwarded headers from
    /// </summary>
    public string KnownNetworks { get; protected set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to set the path base from the "X-Forwarded-Prefix" header
    /// </summary>
    /// <remarks>
    /// Requires <see cref="UseProxy"/>. Use it when the reverse proxy strips the prefix before forwarding
    /// the request (and thus owns the value) instead of setting a fixed <see cref="PathBase"/>.
    /// The header is only honored for requests coming from <see cref="KnownProxies"/>/<see cref="KnownNetworks"/>;
    /// without them the source cannot be verified and the header is ignored, because a spoofed one would
    /// alter the generated URLs.
    /// </remarks>
    public bool UseForwardedPrefix { get; protected set; }

    /// <summary>
    /// Gets or sets a custom path base the application is hosted under, for example "/shop"
    /// </summary>
    /// <remarks>
    /// Empty or "/" means the application is hosted at the root of the host, which is the default.
    /// </remarks>
    public string PathBase { get; protected set; } = "/";

}
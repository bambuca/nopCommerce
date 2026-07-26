//This product includes GeoLite2 data created by MaxMind, available from http://www.maxmind.com
//more info: http://maxmind.github.io/GeoIP2-dotnet/
//more info: https://github.com/maxmind/GeoIP2-dotnet
//more info: http://dev.maxmind.com/geoip/geoip2/geolite2/

using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using MaxMind.GeoIP2.Responses;
using Nop.Core.Infrastructure;
using Nop.Services.Logging;

namespace Nop.Services.Directory;

/// <summary>
/// GEO lookup service
/// </summary>
/// <remarks>
/// Registered as a singleton (see <c>NopStartup</c>) so the underlying
/// <see cref="DatabaseReader"/> is created once and reused for the lifetime of the application.
/// MaxMind's <see cref="DatabaseReader"/> is documented as thread-safe and designed for reuse;
/// per-call instantiation leaks a memory-mapped file handle until the finalizer runs, which under
/// load is too slow and exhausts the process file descriptor limit.
/// </remarks>
public partial class GeoLookupService : IGeoLookupService, IDisposable
{
    #region Fields

    protected readonly ILogger _logger;
    protected readonly INopFileProvider _fileProvider;

    private readonly Lazy<DatabaseReader> _reader;
    private bool _disposed;

    #endregion

    #region Ctor

    public GeoLookupService(ILogger logger,
        INopFileProvider fileProvider)
    {
        _logger = logger;
        _fileProvider = fileProvider;
        _reader = new Lazy<DatabaseReader>(CreateReader, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Create the underlying MaxMind reader. Factored out so subclasses (and tests) can override
    /// the database location without re-implementing the lifecycle.
    /// </summary>
    /// <returns>Database reader</returns>
    protected virtual DatabaseReader CreateReader()
    {
        //This product includes GeoLite2 data created by MaxMind, available from http://www.maxmind.com
        var databasePath = _fileProvider.MapPath(NopDirectoryDefaults.GeoLiteCountryDatabasePath);
        return new DatabaseReader(databasePath);
    }

    /// <summary>
    /// Get information
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Information</returns>
    protected virtual CountryResponse GetInformation(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return null;

        try
        {
            return _reader.Value.Country(ipAddress);
        }
        //catch (AddressNotFoundException exc)
        catch (GeoIP2Exception)
        {
            //address is not found
            //do not throw exceptions
            return null;
        }
        catch (Exception exc)
        {
            //do not throw exceptions
            _logger.Warning("Cannot load MaxMind record", exc);
            return null;
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Get country ISO code
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Country name</returns>
    public virtual string LookupCountryIsoCode(string ipAddress)
    {
        var response = GetInformation(ipAddress);
        if (response?.Country != null)
            return response.Country.IsoCode;

        return string.Empty;
    }

    /// <summary>
    /// Get country name
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Country name</returns>
    public virtual string LookupCountryName(string ipAddress)
    {
        var response = GetInformation(ipAddress);
        if (response?.Country != null)
            return response.Country.Name;

        return string.Empty;
    }

    /// <summary>
    /// Dispose GEO lookup service
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing && _reader.IsValueCreated)
            _reader.Value.Dispose();

        _disposed = true;
    }

    #endregion
}

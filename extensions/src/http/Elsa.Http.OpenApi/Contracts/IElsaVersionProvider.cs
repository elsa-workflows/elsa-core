using System.Reflection;

namespace Elsa.Http.OpenApi.Contracts;

/// <summary>
/// Service for retrieving Elsa package version information.
/// </summary>
public interface IElsaVersionProvider
{
    /// <summary>
    /// Gets the Elsa package version.
    /// </summary>
    /// <returns>The package version string.</returns>
    string GetVersion();
}

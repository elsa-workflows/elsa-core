using Elsa.Features.Models;

namespace Elsa.Features.Contracts;

/// <summary>
/// Provides read-only access to installed features.
/// </summary>
/// <remarks>
/// This interface is used to query the list of features that are installed and available in the application.
/// Unlike <see cref="IInstalledFeatureRegistry"/>, this interface does not provide write operations,
/// making it suitable for implementations where features are discovered automatically (e.g., from shell features)
/// rather than manually registered.
/// </remarks>
public interface IInstalledFeatureProvider
{
    /// <summary>
    /// Gets all installed features.
    /// </summary>
    /// <returns>All installed features.</returns>
    IEnumerable<FeatureDescriptor> List();

    /// <summary>
    /// Finds a feature descriptor by its full name.
    /// </summary>
    /// <param name="fullName">The full name of the feature.</param>
    /// <returns>The feature descriptor or null if not found.</returns>
    FeatureDescriptor? Find(string fullName);
}

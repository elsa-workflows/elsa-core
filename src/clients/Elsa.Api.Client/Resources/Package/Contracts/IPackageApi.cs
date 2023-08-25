using Elsa.Api.Client.Resources.Package.Models;
using Refit;

namespace Elsa.Api.Client.Resources.Package.Contracts;

/// <summary>
/// A client for working with packages.
/// </summary>
public interface IPackageApi
{
    /// <summary>
    /// Gets the installed package version of Elsa.
    /// </summary>
    [Get("/package/version")]
    Task<PackageVersionResponse> GetAsync(CancellationToken cancellationToken = default);   
}
namespace Elsa.Api.Client.Resources.Package.Models;

/// <summary>
/// Contains the installed package version of Elsa.
/// </summary>
/// <param name="PackageVersion">The installed package version of Elsa.</param>
public record PackageVersionResponse(string PackageVersion);
namespace Elsa.Studio.Models;

/// <summary>
/// Contains information about the server.
/// </summary>
/// <param name="PackageVersion">The installed package version of Elsa.</param>
public record ServerInformation(string PackageVersion);
namespace Elsa.Studio.Models;

/// <summary>
/// Contains information about the client.
/// </summary>
/// <param name="PackageVersion">The installed package version of Elsa Studio.</param>
public record ClientInformation(string PackageVersion);
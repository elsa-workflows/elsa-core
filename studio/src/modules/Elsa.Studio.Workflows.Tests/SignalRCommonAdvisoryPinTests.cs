using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Studio's SignalR.Client pin pulls Microsoft.AspNetCore.SignalR.Common of the same version.
/// GHSA-334f-vvp4-7g6p: 10.0.0-10.0.12, 9.0.0-9.0.20, and 8.0.0-8.0.31 are affected;
/// first patched versions are 10.0.13, 9.0.21, and 8.0.32.
/// </summary>
public class SignalRCommonAdvisoryPinTests
{
    private const string PackageId = "Microsoft.AspNetCore.SignalR.Common";
    private static readonly Version FirstPatchedNet10 = new(10, 0, 13);
    private static readonly Version LastAffectedNet8 = new(8, 0, 31);
    private static readonly Version LastAffectedNet9 = new(9, 0, 20);

    [Fact]
    public void RestoreGraph_PinsSignalRCommonOutsideAdvisoryRanges()
    {
        var versions = RestoredPackageVersions(PackageId);
        Assert.NotEmpty(versions);
        Assert.All(versions, version =>
            Assert.True(
                IsPatched(version),
                $"{PackageId} {version} is still in GHSA-334f-vvp4-7g6p."));
    }

    [Fact]
    public void LoadedSignalRClient_MeetsNet10AdvisoryFloor()
    {
        var informational = typeof(HubConnection).Assembly
                                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                            ?? throw new InvalidOperationException("SignalR.Client informational version is missing.");
        var version = Version.Parse(informational.Split('+', '-')[0]);
        Assert.True(
            version >= FirstPatchedNet10,
            $"Loaded SignalR.Client {informational} is still in GHSA-334f-vvp4-7g6p (10.0.0-10.0.12).");
    }

    private static bool IsPatched(Version version) =>
        version >= FirstPatchedNet10
        || (version.Major == 9 && version > LastAffectedNet9)
        || (version.Major == 8 && version > LastAffectedNet8);

    private static IReadOnlyList<Version> RestoredPackageVersions(string packageId)
    {
        var libraries = JsonDocument.Parse(File.ReadAllText(FindAssetsPath())).RootElement.GetProperty("libraries");
        return libraries.EnumerateObject()
            .Where(property => property.Name.StartsWith(packageId + "/", StringComparison.OrdinalIgnoreCase))
            .Select(property => Version.Parse(property.Name[(packageId.Length + 1)..].Split('-', '+')[0]))
            .ToList();
    }

    private static string FindAssetsPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var assets = Path.Combine(directory.FullName, "obj", "project.assets.json");
            if (File.Exists(assets))
                return assets;
        }

        throw new FileNotFoundException("project.assets.json", AppContext.BaseDirectory);
    }
}

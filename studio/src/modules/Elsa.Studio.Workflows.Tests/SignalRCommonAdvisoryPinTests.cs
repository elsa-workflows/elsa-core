using System.Reflection;
using Elsa.Testing.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Studio's SignalR.Client pin pulls Microsoft.AspNetCore.SignalR.Common of the same version.
/// GHSA-334f-vvp4-7g6p: 8.0.0-8.0.31, 9.0.0-9.0.20, and 10.0.0-10.0.12 are affected;
/// first patched versions are 8.0.32, 9.0.21, and 10.0.13.
/// </summary>
public class SignalRCommonAdvisoryPinTests
{
    private const string PackageId = "Microsoft.AspNetCore.SignalR.Common";

    [Fact]
    public void RestoreGraph_PinsSignalRCommonToTfmPatchedVersion()
    {
        var versions = RestoredPackageVersionReader.Read(PackageId);
        Assert.NotEmpty(versions);
        Assert.All(versions, version => Assert.Equal(ExpectedPatchedVersion, version));
    }

    [Fact]
    public void LoadedSignalRClient_MeetsTfmAdvisoryFloor()
    {
        var informational = typeof(HubConnection).Assembly
                                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                            ?? throw new InvalidOperationException("SignalR.Client informational version is missing.");
        var version = Version.Parse(informational.Split('+', '-')[0]);
        Assert.True(
            version >= ExpectedPatchedVersion,
            $"Loaded SignalR.Client {informational} is still in GHSA-334f-vvp4-7g6p (expected {ExpectedPatchedVersion}).");
    }

    private static Version ExpectedPatchedVersion
    {
        get
        {
#if NET8_0
            return new Version(8, 0, 32);
#elif NET9_0
            return new Version(9, 0, 21);
#else
            return new Version(10, 0, 13);
#endif
        }
    }
}

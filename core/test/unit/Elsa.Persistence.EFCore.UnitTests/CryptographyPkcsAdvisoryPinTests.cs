using Elsa.Testing.Shared;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// SqlClient and Oracle pull System.Security.Cryptography.Pkcs into this graph.
/// GHSA-2fmv-rv2c-59x7 and GHSA-846f-f764-76mx: 8.0.0-8.0.1, 9.0.0-9.0.20, and 10.0.0-10.0.12
/// are affected. First patched versions are 8.0.2, 9.0.21, and 10.0.13.
/// </summary>
public class CryptographyPkcsAdvisoryPinTests
{
    private const string PackageId = "System.Security.Cryptography.Pkcs";

    [Fact]
    public void RestoreGraph_PinsPkcsToTfmPatchedVersion()
    {
        var versions = RestoredPackageVersionReader.Read(PackageId);
        Assert.NotEmpty(versions);
        Assert.All(versions, version => Assert.Equal(ExpectedPatchedVersion, version));
    }

#if NET8_0
    [Fact]
    public void RestoreGraph_SqlServerNet8Asn1ResolvesEfFloorNotCpm802()
    {
        // Microsoft.EntityFrameworkCore.SqlServer requires System.Formats.Asn1 >= 9.0.17 on net8.
        // That is higher than the CPM 8.0.2 floor and is not in an Asn1 advisory. Do not claim 8.0.2 resolves here.
        var versions = RestoredPackageVersionReader.Read("System.Formats.Asn1");
        Assert.Contains(new Version(9, 0, 17), versions);
        Assert.DoesNotContain(versions, version => version == new Version(8, 0, 2));
    }
#endif

    private static Version ExpectedPatchedVersion
    {
        get
        {
#if NET8_0
            return new Version(8, 0, 2);
#elif NET9_0
            return new Version(9, 0, 21);
#else
            return new Version(10, 0, 13);
#endif
        }
    }
}

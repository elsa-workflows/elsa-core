using System.Security.Cryptography;
using Elsa.Secrets;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Secrets.UnitTests;

public class LegacyDataProtectionContextTests
{
    private const string ApplicationName = "Elsa-Secrets-Bridge-Contract-Synthetic";
    // Extensions 3.8.1 DataProtectionEncryptor, commit 01bf9ad70d0399afadae9609fe820703d3de290d.
    private const string Purpose = "Elsa.Secrets.Encryption";

    [Fact]
    public void LegacyBridgeContext_RequiresMatchingApplicationNameAndPurpose()
    {
        var keyRingDirectory = Directory.CreateTempSubdirectory("elsa-secrets-legacy-dp-");

        try
        {
            using var correctServices = CreateDataProtectionServices(keyRingDirectory, ApplicationName);
            using var wrongApplicationServices = CreateDataProtectionServices(keyRingDirectory, "Elsa-Secrets-Bridge-Contract-Wrong-Context");
            var protectedValue = correctServices.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(Purpose)
                .Protect("synthetic legacy secret");

            var recoveredValue = correctServices.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(Purpose)
                .Unprotect(protectedValue);

            Assert.Equal("synthetic legacy secret", recoveredValue);
            Assert.Throws<CryptographicException>(() => wrongApplicationServices.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(Purpose)
                .Unprotect(protectedValue));
            Assert.Throws<CryptographicException>(() => correctServices.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("Elsa.Secrets.Encryption.WrongPurpose")
                .Unprotect(protectedValue));
        }
        finally
        {
            keyRingDirectory.Delete(recursive: true);
        }
    }

    private static ServiceProvider CreateDataProtectionServices(DirectoryInfo keyRingDirectory, string applicationName)
    {
        return new ServiceCollection()
            .AddDataProtection()
            .PersistKeysToFileSystem(keyRingDirectory)
            .SetApplicationName(applicationName)
            .Services
            .BuildServiceProvider();
    }
}

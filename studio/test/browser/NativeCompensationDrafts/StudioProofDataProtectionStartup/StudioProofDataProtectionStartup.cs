using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(Elsa.NativeCompensation.StudioProofStartup.StudioProofDataProtectionStartup))]

namespace Elsa.NativeCompensation.StudioProofStartup;

/// <summary>
/// Fixture-only Data Protection setup for an isolated Server Studio proof process.
/// </summary>
public sealed class StudioProofDataProtectionStartup : IHostingStartup
{
    public const string KeyDirectoryVariable = "ELSA_COMPENSATION_STUDIO_DP_KEY_DIR";
    private const string ApplicationName = "Elsa.NativeCompensation.StudioProof";
    private const string LoadedMarker = ".studio-proof-hosting-startup-loaded";

    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var configuredPath = Environment.GetEnvironmentVariable(KeyDirectoryVariable);
            if (string.IsNullOrWhiteSpace(configuredPath) || !Path.IsPathFullyQualified(configuredPath))
                throw new InvalidOperationException("The isolated Studio proof requires an absolute temporary Data Protection key directory.");

            var keyDirectory = new DirectoryInfo(Path.GetFullPath(configuredPath));
            if (!keyDirectory.Exists)
                throw new InvalidOperationException("The isolated Studio proof Data Protection key directory must already exist.");

            services.AddDataProtection()
                .SetApplicationName(ApplicationName)
                .PersistKeysToFileSystem(keyDirectory);

            // The process supervisor uses this value-free marker to confirm that the startup assembly loaded.
            File.WriteAllText(Path.Combine(keyDirectory.FullName, LoadedMarker), string.Empty);
        });
    }
}

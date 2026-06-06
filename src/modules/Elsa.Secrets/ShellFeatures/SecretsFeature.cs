using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Secrets.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.ShellFeatures;

[ShellFeature(
    DisplayName = "Secrets",
    Description = "Provides named secret management, secret stores, and runtime secret resolution.",
    DependsOn = [typeof(global::Elsa.ShellFeatures.ElsaFastEndpointsFeature)])]
[UsedImplicitly]
public class SecretsFeature : IFastEndpointsShellFeature
{
    public string ConfigurationSectionName { get; set; } = "Elsa:Secrets";
    public byte[]? EncryptionKey { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSecretsServices(options =>
        {
            options.ConfigurationSectionName = ConfigurationSectionName;
            if (EncryptionKey != null)
                options.EncryptionKey = EncryptionKey;
        });
    }
}

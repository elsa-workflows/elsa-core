using CShells.Configuration;
using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.ExternalAuthentication.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.ShellFeatures;

[ShellFeature(
    DisplayName = "External Authentication",
    Description = "Provides brokered external authentication and identity provider connection management.",
    DependsOn = ["SystemClock", "ElsaFastEndpoints"])]
[UsedImplicitly]
public sealed class ExternalAuthenticationShellFeature : IFastEndpointsShellFeature
{
    public string ConfigurationSectionName { get; set; } = "ExternalAuthentication";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddExternalAuthenticationServices();
        services.AddOptions<ExternalAuthenticationOptions>()
            .Configure<ShellConfiguration>((options, configuration) => configuration.GetSection(ConfigurationSectionName).Bind(options));
    }
}

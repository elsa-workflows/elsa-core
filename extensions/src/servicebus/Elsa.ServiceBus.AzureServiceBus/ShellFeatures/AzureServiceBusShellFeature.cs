using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.AzureServiceBus.ShellFeatures;

/// <summary>
/// Shell feature for Azure Service Bus message bus.
/// </summary>
[ShellFeature(
    DisplayName = "Azure Service Bus",
    Description = "Enables Azure Service Bus for message publishing and handling")]
[UsedImplicitly]
[ManifestInfrastructure("azure-service-bus", "service-bus", Reason = "Publishes and consumes workflow messages through Azure Service Bus.", Providers = new[] { "Azure Service Bus" }, ConfigurationKeys = new[] { "ConnectionStringOrName" })]
public class AzureServiceBusShellFeature : IShellFeature
{
    [ManifestSetting(
        DisplayName = "Connection string or name",
        Description = "The Azure Service Bus connection string or configured connection string name.",
        Category = "Connection",
        Secret = true,
        Required = true,
        HasRequired = true,
        RestartRequired = true)]
    public string ConnectionStringOrName { get; set; } = string.Empty;

    public void ConfigureServices(IServiceCollection services)
    {
        // Service registration for Azure Service Bus
        services.AddOptions<Options.AzureServiceBusOptions>()
            .Configure(options => options.ConnectionStringOrName = ConnectionStringOrName);
    }
}

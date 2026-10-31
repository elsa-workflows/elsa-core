using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.Kafka.ShellFeatures;

/// <summary>
/// Shell feature for Kafka message bus.
/// </summary>
[ShellFeature(
    DisplayName = "Kafka Service Bus",
    Description = "Enables Apache Kafka for message publishing and handling")]
[UsedImplicitly]
[ManifestInfrastructure("kafka-broker", "message-broker", Reason = "Publishes and consumes workflow messages through Apache Kafka.", Providers = new[] { "Apache Kafka" }, ConfigurationKeys = new[] { "WorkflowInstanceIdHeaderKey" })]
public class KafkaShellFeature : IShellFeature
{
    [ManifestSetting(
        DisplayName = "Workflow instance ID header key",
        Description = "The Kafka message header key used to carry the workflow instance ID.",
        Category = "Headers",
        RestartRequired = true)]
    public string WorkflowInstanceIdHeaderKey { get; set; } = "x-workflow-instance-id";

    public void ConfigureServices(IServiceCollection services)
    {
        // Kafka configuration setup
        services.AddOptions<KafkaOptions>()
            .Configure(options =>
            {
                options.WorkflowInstanceIdHeaderKey = WorkflowInstanceIdHeaderKey;
            });
    }
}

using Elsa.Agents;
using Elsa.Agents.Activities.ActivityProviders;
using Elsa.Expressions.Contracts;
using Elsa.OrchardCore.ActivityProviders;
using Elsa.OrchardCore.Options;
using Elsa.ServiceBus.MassTransit.Options;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Telnyx.Providers;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

internal static class GeneratedActivityFixtures
{
    public static async Task<IReadOnlyList<(string Provider, ActivityDescriptor Descriptor)>> DescribeAsync(IServiceProvider services)
    {
        var describer = services.GetRequiredService<IActivityDescriber>();
        IActivityProvider[] providers =
        [
            new AgentActivityProvider(new FixtureKernelConfigProvider(), describer, services.GetRequiredService<IWellKnownTypeRegistry>()),
            new OrchardContentItemsEventActivityProvider(Options.Create(new OrchardCoreOptions { ContentTypes = new HashSet<string> { "CompatibilityArticle" } }), describer),
            new MassTransitActivityTypeProvider(Options.Create(new MassTransitActivityOptions { MessageTypes = new HashSet<Type> { typeof(CompatibilityMessage) } }), describer),
            new WebhookEventActivityProvider(describer)
        ];
        var result = new List<(string, ActivityDescriptor)>();
        foreach (var provider in providers)
        {
            foreach (var descriptor in await provider.GetDescriptorsAsync())
            {
                result.Add((provider.GetType().FullName!, descriptor));
            }
        }
        return result;
    }

    private sealed class FixtureKernelConfigProvider : IKernelConfigProvider
    {
        public Task<KernelConfig> GetKernelConfigAsync(CancellationToken cancellationToken = default)
        {
            var config = new KernelConfig();
            config.Agents.Add("compatibility-agent", new AgentConfig
            {
                Name = "compatibility-agent",
                FunctionName = "Summarize",
                Description = "Offline descriptor fixture; never invokes an agent.",
                InputVariables = [new InputVariableConfig { Name = "Text", Type = "String" }, new InputVariableConfig { Name = "Count", Type = "Int32" }],
                OutputVariable = new OutputVariableConfig { Type = "String" }
            });
            return Task.FromResult(config);
        }
    }
}

public sealed record CompatibilityMessage(string Text);

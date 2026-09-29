using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Humanizer;
using JetBrains.Annotations;

namespace Elsa.Agents.Activities.ActivityProviders;

/// <summary>
/// Provides activities for each registered agent.
/// </summary>
[UsedImplicitly]
public class AgentActivityProvider(
    IKernelConfigProvider kernelConfigProvider,
    IActivityDescriber activityDescriber,
    IWellKnownTypeRegistry wellKnownTypeRegistry
) : IActivityProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var kernelConfig = await kernelConfigProvider.GetKernelConfigAsync(cancellationToken);
        var activityDescriptors = new List<ActivityDescriptor>();

        // Add descriptors for individual agents
        foreach (var kvp in kernelConfig.Agents)
        {
            var agentConfig = kvp.Value;
            var descriptor = await CreateAgentActivityDescriptor(agentConfig, cancellationToken);
            activityDescriptors.Add(descriptor);
        }

        return activityDescriptors;
    }

    private async Task<ActivityDescriptor> CreateAgentActivityDescriptor(AgentConfig agentConfig, CancellationToken cancellationToken)
    {
        var activityDescriptor = await activityDescriber.DescribeActivityAsync(typeof(AgentActivity), cancellationToken);
        var functionName = string.IsNullOrWhiteSpace(agentConfig.FunctionName) ? agentConfig.Name : agentConfig.FunctionName;
        var activityTypeName = $"Elsa.Agents.{activityDescriptor.Name.Pascalize()}.{functionName.Pascalize()}";
        activityDescriptor.Name = functionName.Pascalize();
        activityDescriptor.TypeName = activityTypeName;
        activityDescriptor.Description = agentConfig.Description;
        activityDescriptor.DisplayName = functionName.Humanize().Transform(To.TitleCase);
        activityDescriptor.IsBrowsable = true;
        activityDescriptor.Category = "Agents";
        activityDescriptor.Kind = ActivityKind.Task;
        activityDescriptor.RunAsynchronously = true;
        activityDescriptor.ClrType = typeof(AgentActivity);

        activityDescriptor.Constructor = context =>
        {
            var result = context.CreateActivity<AgentActivity>();
            var activity = result.Activity;
            activity.Type = activityTypeName;
            activity.AgentName = agentConfig.Name;
            activity.RunAsynchronously = true;
            return result;
        };

        activityDescriptor.Inputs.Clear();

        foreach (var inputVariable in agentConfig.InputVariables)
        {
            var inputName = inputVariable.Name;
            var inputType = inputVariable.Type == null! ? "object" : inputVariable.Type;
            var nakedInputType = wellKnownTypeRegistry.GetTypeOrDefault(inputType);
            var inputDescriptor = new InputDescriptor
            {
                Name = inputVariable.Name,
                DisplayName = inputVariable.Name.Humanize(),
                Description = inputVariable.Description,
                Type = nakedInputType,
                ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(inputName),
                ValueSetter = (activity, value) => activity.SyntheticProperties[inputName] = value!,
                IsSynthetic = true,
                IsWrapped = true,
                UIHint = ActivityDescriber.GetUIHint(nakedInputType)
            };
            activityDescriptor.Inputs.Add(inputDescriptor);
        }

        activityDescriptor.Outputs.Clear();
        var outputVariable = agentConfig.OutputVariable;
        var outputType = outputVariable.Type == null! ? "object" : outputVariable.Type;
        var nakedOutputType = wellKnownTypeRegistry.GetTypeOrDefault(outputType);
        var outputName = "Output";
        var outputDescriptor = new OutputDescriptor
        {
            Name = outputName,
            Description = agentConfig.OutputVariable.Description,
            Type = nakedOutputType,
            IsSynthetic = true,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(outputName),
            ValueSetter = (activity, value) => activity.SyntheticProperties[outputName] = value!,
        };
        activityDescriptor.Outputs.Add(outputDescriptor);

        return activityDescriptor;
    }
}
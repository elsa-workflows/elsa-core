using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Agents.Activities.ActivityProviders;

/// Provides activities for each function of registered agents.
[UsedImplicitly]
public class AgentActivityProvider(
    IKernelConfigProvider kernelConfigProvider,
    IActivityDescriber activityDescriber,
    IActivityFactory activityFactory,
    IWellKnownTypeRegistry wellKnownTypeRegistry
) : IActivityProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var kernelConfig = await kernelConfigProvider.GetKernelConfigAsync(cancellationToken);
        var agents = kernelConfig.Agents;
        var activityDescriptors = new List<ActivityDescriptor>();

        foreach (var kvp in agents)
        {
            var agentConfig = kvp.Value;
            var activityDescriptor = await activityDescriber.DescribeActivityAsync(typeof(AgentActivity), cancellationToken);
            var activityTypeName = $"Elsa.Agents.{agentConfig.Name.Pascalize()}";
            activityDescriptor.Name = agentConfig.Name.Pascalize();
            activityDescriptor.TypeName = activityTypeName;
            activityDescriptor.Description = agentConfig.Description;
            activityDescriptor.DisplayName = agentConfig.Name.Humanize().Transform(To.TitleCase);
            activityDescriptor.IsBrowsable = true;
            activityDescriptor.Category = "Agents";
            activityDescriptor.Kind = ActivityKind.Job;
            activityDescriptor.CustomProperties["RootType"] = nameof(AgentActivity);

            activityDescriptor.Constructor = context =>
            {
                var activity = (AgentActivity)activityFactory.Create(typeof(AgentActivity), context);
                activity.Type = activityTypeName;
                activity.AgentName = agentConfig.Name;
                return activity;
            };

            activityDescriptors.Add(activityDescriptor);
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
        }

        return activityDescriptors;
    }
}
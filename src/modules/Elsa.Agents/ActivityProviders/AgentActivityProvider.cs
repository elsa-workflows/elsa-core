using Elsa.Agents.Activities;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Agents.ActivityProviders;

/// Provides activities for each function of registered agents.
[UsedImplicitly]
public class AgentActivityProvider(AgentManager agentManager, KernelConfig kernelConfig, IActivityDescriber activityDescriber, IActivityFactory activityFactory, IWellKnownTypeRegistry wellKnownTypeRegistry, ILogger<AgentActivityProvider> logger) : IActivityProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var agents = agentManager.GetAgents();
        var activityDescriptors = new List<ActivityDescriptor>();

        foreach (var agent in agents)
        {
            var agentConfig = agent.AgentConfig;
            var skills = agentConfig.Skills;

            foreach (var skillName in skills)
            {
                if (!kernelConfig.Skills.TryGetValue(skillName, out var skill))
                {
                    logger.LogWarning($"Skill {skillName} not found");
                    continue;
                }

                var functions = skill.Functions;

                foreach (var functionConfig in functions)
                {
                    var activityDescriptor = await activityDescriber.DescribeActivityAsync(typeof(AgentActivity), cancellationToken);
                    var activityTypeName = $"ElsaX.Agents.{agent.Name.Pascalize()}.{skillName.Pascalize()}.{functionConfig.FunctionName.Pascalize()}";
                    activityDescriptor.Name = $"{agent.Name}:{skillName}:{functionConfig.FunctionName}";
                    activityDescriptor.TypeName = activityTypeName;
                    activityDescriptor.Description = functionConfig.Description;
                    activityDescriptor.DisplayName = $"{agent.Name}: {functionConfig.FunctionName.Humanize()}";
                    activityDescriptor.IsBrowsable = true;
                    activityDescriptor.Category = "Agent Skills";
                    activityDescriptor.Kind = ActivityKind.Task;

                    activityDescriptor.Constructor = context =>
                    {
                        var activity = (AgentActivity)activityFactory.Create(typeof(AgentActivity), context);
                        activity.Type = activityTypeName;
                        activity.Function = functionConfig.FunctionName;
                        activity.Skill = skillName;
                        activity.Agent = agent;
                        return activity;
                    };

                    activityDescriptors.Add(activityDescriptor);
                    activityDescriptor.Inputs.Clear();

                    foreach (var inputVariable in functionConfig.InputVariables)
                    {
                        var inputName = inputVariable.Name;
                        var inputType = inputVariable.Type == null! ? "object" : inputVariable.Type;
                        var nakedInputType = wellKnownTypeRegistry.GetTypeOrDefault(inputType);
                        var inputDescriptor = new InputDescriptor
                        {
                            Name = inputVariable.Name,
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
                    var outputType = functionConfig.OutputVariable.Type == null! ? "object" : functionConfig.OutputVariable.Type;
                    var nakedOutputType = wellKnownTypeRegistry.GetTypeOrDefault(outputType);
                    var outputName = "Output";
                    var outputDescriptor = new OutputDescriptor
                    {
                        Name = outputName,
                        Description = functionConfig.OutputVariable.Description,
                        Type = nakedOutputType,
                        IsSynthetic = true,
                        ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(outputName),
                        ValueSetter = (activity, value) => activity.SyntheticProperties[outputName] = value!,
                    };
                    activityDescriptor.Outputs.Add(outputDescriptor);
                }
            }
        }

        return activityDescriptors;
    }
}
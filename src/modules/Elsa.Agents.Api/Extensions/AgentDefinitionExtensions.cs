using Elsa.Agents;
using Elsa.Agents.Persistence.Entities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class AgentDefinitionExtensions
{
    public static AgentModel ToModel(this AgentDefinition agentDefinition)
    {
        return new AgentModel
        {
            Id = agentDefinition.Id,
            Name = agentDefinition.Name,
            Description = agentDefinition.Description,
            Agents = agentDefinition.AgentConfig.Agents,
            ExecutionSettings = agentDefinition.AgentConfig.ExecutionSettings,
            InputVariables = agentDefinition.AgentConfig.InputVariables,
            OutputVariable = agentDefinition.AgentConfig.OutputVariable,
            Services = agentDefinition.AgentConfig.Services,
            Plugins = agentDefinition.AgentConfig.Plugins,
            FunctionName = agentDefinition.AgentConfig.FunctionName,
            PromptTemplate = agentDefinition.AgentConfig.PromptTemplate
        };
    }
}
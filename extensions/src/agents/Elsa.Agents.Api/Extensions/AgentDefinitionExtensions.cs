using Elsa.Agents;
using Elsa.Agents.Persistence.Entities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class AgentDefinitionExtensions
{
    public static AgentModel ToModel(this AgentDefinition agentDefinition)
    {
        return new()
        {
            Id = agentDefinition.Id,
            Name = agentDefinition.Name,
            FunctionName = agentDefinition.AgentConfig.FunctionName,
            Description = agentDefinition.Description,
            Agents = agentDefinition.AgentConfig.Agents,
            ExecutionSettings = agentDefinition.AgentConfig.ExecutionSettings,
            InputVariables = agentDefinition.AgentConfig.InputVariables,
            OutputVariable = agentDefinition.AgentConfig.OutputVariable,
            Skills = agentDefinition.AgentConfig.Skills,
            PromptTemplate = agentDefinition.AgentConfig.PromptTemplate
        };
    }
}
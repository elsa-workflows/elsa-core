using Elsa.Agents;

namespace Elsa.Agents.Examples;

/// <summary>
/// Example: Simple code-first agent definition
/// </summary>
public class GreeterAgent : IAgentDefinition
{
    public string Name => "GreeterAgent";
    public string Description => "Greets users warmly";

    public AgentConfig GetAgentConfig()
    {
        return new AgentConfig
        {
            Name = Name,
            Description = Description,
            Services = ["OpenAIChat"],
            FunctionName = "Greet",
            PromptTemplate = "Greet the user by name: {{userName}}",
            InputVariables =
            [
                new InputVariableConfig { Name = "userName", Type = "String", Description = "User name" }
            ],
            OutputVariable = new OutputVariableConfig { Type = "String", Description = "Greeting" }
        };
    }
}

/// <summary>
/// Example: Multi-agent workflow definition
/// </summary>
public class ContentWorkflow : IAgentWorkflowDefinition
{
    public string Name => "ContentPipeline";
    public string Description => "Multi-agent content creation";

    public AgentWorkflowConfig GetWorkflowConfig()
    {
        return new AgentWorkflowConfig
        {
            Name = Name,
            Description = Description,
            WorkflowType = AgentWorkflowType.Sequential,
            Agents = ["Researcher", "Writer", "Editor"],
            Services = ["OpenAIChat"],
            InputVariables = [new InputVariableConfig { Name = "topic", Type = "String" }],
            OutputVariable = new OutputVariableConfig { Type = "String" },
            Termination = new TerminationConfig { Type = TerminationType.MaxMessages, MaxMessages = 15 }
        };
    }
}

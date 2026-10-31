# Elsa Agents Module

The Elsa Agents module provides an agentic framework built on top of Microsoft's Agent Framework (Semantic Kernel Agents), enabling you to define and execute AI agents and multi-agent workflows as Elsa workflow activities.

## Features

- **JSON/Configuration-Based Agents**: Define agents via configuration (appsettings.json, database)
- **Code-First Agents**: Register agents programmatically using fluent APIs
- **Multi-Agent Workflows**: Orchestrate multiple agents working together (sequential, graph-based)
- **Workflow Integration**: All agents and agent workflows are exposed as Elsa workflow activities
- **Tool Calling**: Agents can invoke tools/plugins and other agents
- **Memory and State**: Support for persistent conversations and state management

## Architecture

The module consists of several packages:

- **Elsa.Agents.Core**: Core services, abstractions, and Agent Framework integration
- **Elsa.Agents.Models**: Configuration models and data contracts
- **Elsa.Agents.Activities**: Workflow activity implementations and providers
- **Elsa.Agents.Api**: REST API endpoints for agent management
- **Elsa.Agents.Persistence**: Database persistence abstractions
- **Elsa.Studio.Agents**: Blazor UI for managing agents

## Getting Started

### Installation

```bash
dotnet add package Elsa.Agents.Core
dotnet add package Elsa.Agents.Activities
```

### Configuration

Add agents to your Elsa configuration:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseAgents(agents =>
    {
        // Register service providers (OpenAI, Azure OpenAI, etc.)
        agents.UseOpenAI();
        
        // Enable agent activities
        elsa.UseAgentActivities();
    });
});
```

## Defining Agents

### 1. JSON/Configuration-Based Agents

Define agents in `appsettings.json`:

```json
{
  "Agents": {
    "ApiKeys": [
      {
        "Name": "OpenAI",
        "Value": "sk-..."
      }
    ],
    "Services": [
      {
        "Name": "OpenAIChat",
        "Type": "OpenAIChatCompletion",
        "ApiKeyName": "OpenAI",
        "Model": "gpt-4"
      }
    ],
    "Agents": [
      {
        "Name": "CustomerSupport",
        "Description": "Helpful customer support agent",
        "Services": ["OpenAIChat"],
        "FunctionName": "HandleCustomerQuery",
        "PromptTemplate": "You are a helpful customer support agent. Help the user with their question: {{question}}",
        "InputVariables": [
          {
            "Name": "question",
            "Type": "String",
            "Description": "The customer's question"
          }
        ],
        "OutputVariable": {
          "Type": "String",
          "Description": "The agent's response"
        },
        "Plugins": ["WebSearch", "KnowledgeBase"]
      }
    ]
  }
}
```

### 2. Code-First Agents

Register agents programmatically:

```csharp
// Simple agent definition
services.AddElsa(elsa =>
{
    elsa.UseAgents(agents =>
    {
        agents.Services.AddAgentDefinition(new SimpleAgentDefinition
        {
            Name = "GreeterAgent",
            Description = "Greets users warmly",
            AgentConfig = new AgentConfig
            {
                Name = "GreeterAgent",
                Description = "Greets users warmly",
                Services = ["OpenAIChat"],
                FunctionName = "Greet",
                PromptTemplate = "Greet the user by name: {{userName}}",
                InputVariables = 
                [
                    new InputVariableConfig 
                    { 
                        Name = "userName", 
                        Type = "String",
                        Description = "The user's name" 
                    }
                ],
                OutputVariable = new OutputVariableConfig 
                { 
                    Type = "String", 
                    Description = "Greeting message" 
                }
            }
        });
    });
});

// Or implement IAgentDefinition
public class GreeterAgentDefinition : IAgentDefinition
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
                new InputVariableConfig 
                { 
                    Name = "userName", 
                    Type = "String",
                    Description = "The user's name" 
                }
            ],
            OutputVariable = new OutputVariableConfig 
            { 
                Type = "String", 
                Description = "Greeting message" 
            }
        };
    }
}

// Register it
services.AddElsa(elsa =>
{
    elsa.UseAgents(agents =>
    {
        agents.Services.AddAgentDefinition<GreeterAgentDefinition>();
    });
});
```

### 3. Agent Workflows (Multi-Agent Teams)

Create workflows of multiple agents:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseAgents(agents =>
    {
        agents.Services.AddAgentWorkflowDefinition(new AgentWorkflowDefinition
        {
            Name = "ContentPipeline",
            Description = "Multi-agent content creation pipeline",
            WorkflowConfig = new AgentWorkflowConfig
            {
                Name = "ContentPipeline",
                Description = "Multi-agent content creation pipeline",
                WorkflowType = AgentWorkflowType.Sequential,
                Agents = ["Researcher", "Writer", "Editor", "SEOSpecialist"],
                Services = ["OpenAIChat"],
                InputVariables = 
                [
                    new InputVariableConfig 
                    { 
                        Name = "topic", 
                        Type = "String",
                        Description = "Content topic" 
                    }
                ],
                OutputVariable = new OutputVariableConfig 
                { 
                    Type = "String", 
                    Description = "Final content" 
                },
                Termination = new TerminationConfig
                {
                    Type = TerminationType.MaxMessages,
                    MaxMessages = 20
                },
                SelectionStrategy = new SelectionStrategyConfig
                {
                    Type = SelectionStrategyType.Sequential
                }
            }
        });
    });
});
```

#### Workflow Types

- **Sequential**: Agents execute in order
- **Graph**: Custom orchestration with agent selection strategies

#### Termination Strategies

- **MaxMessages**: Stop after N messages/turns
- **Keyword**: Terminate when specific keyword is detected
- **AgentDecision**: Let a designated agent decide when to stop

#### Selection Strategies

- **Sequential**: Agents act in order
- **RoundRobin**: Cycle through agents
- **LLMBased**: Use LLM to decide next agent
- **AgentBased**: Dedicated agent makes selection

## Using Agents in Workflows

Once registered, agents automatically appear as activities in the Elsa workflow designer:

### Categories
- **Agents**: Individual agent activities
- **Agent Workflows**: Multi-agent workflow activities

### Activity Inputs/Outputs
Each agent activity has inputs and outputs based on its configuration:

```
CustomerSupport Activity:
  Inputs:
    - question (String): The customer's question
  Outputs:
    - Output (String): The agent's response
```

## Agent Framework Integration

The module uses Microsoft's Agent Framework (Semantic Kernel Agents) for execution:

### Key Components

- **AgentFrameworkFactory**: Creates Agent Framework agents from Elsa configs
- **AgentWorkflowExecutor**: Orchestrates multi-agent workflows
- **AgentInvoker**: Executes individual agents (supports both legacy SK and Agent Framework)

### Backward Compatibility

The `AgentInvoker` supports both:
- **Legacy Mode**: Original Semantic Kernel implementation
- **Agent Framework Mode** (default): New Microsoft Agent Framework

You can force legacy mode for specific scenarios:
```csharp
await agentInvoker.InvokeAgentAsync(agentName, input, useAgentFramework: false, cancellationToken);
```

## Tool Calling

Agents can call:
- **Plugins**: Registered plugins (e.g., WebSearch, ImageGenerator)
- **Other Agents**: Agents can invoke other agents as tools
- **Activities**: Elsa activities exposed as agent tools

Configure tools in agent definition:
```json
{
  "Agents": [
    {
      "Name": "ResearchAgent",
      "Plugins": ["WebSearch", "DocumentQuery"],
      "Agents": ["FactChecker", "Summarizer"]
    }
  ]
}
```

## Service Providers

Supported AI service providers:

- **OpenAI**: Chat completion, embeddings, image generation
- **Azure OpenAI**: Chat completion, embeddings
- Custom providers via `IAgentServiceProvider`

## Database Persistence

Agents can be stored in and loaded from the database:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseAgents(agents =>
    {
        // Use database provider for kernel config
        agents.UseKernelConfigProvider(sp => 
            sp.GetRequiredService<DatabaseKernelConfigProvider>());
    });
});
```

## Examples

See the `Elsa.Studio.Agents/Assets` directory for example agent configurations:
- `hello-world-console.json`: Simple agent example
- `customer-support.json`: Customer support agent
- `content-pipeline.json`: Multi-agent content workflow
- `document-review-process.json`: Document review workflow

## Advanced Topics

### Custom Service Providers

Implement `IAgentServiceProvider`:

```csharp
public class CustomAIProvider : IAgentServiceProvider
{
    public string Name => "CustomAI";
    
    public void ConfigureKernel(KernelBuilderContext context)
    {
        // Configure custom AI service
        context.Builder.Services.AddCustomAIChatCompletion(
            context.ServiceConfig.GetSetting<string>("ApiKey")
        );
    }
}

// Register
services.AddElsa(elsa =>
{
    elsa.UseAgents(agents =>
    {
        agents.Services.AddAgentServiceProvider<CustomAIProvider>();
    });
});
```

### Custom Plugins

Implement agent plugins:

```csharp
public class WeatherPlugin
{
    [KernelFunction, Description("Gets weather for a location")]
    public async Task<string> GetWeather(
        [Description("Location name")] string location)
    {
        // Implementation
        return $"Weather in {location}: Sunny, 72°F";
    }
}

// Register
services.AddElsa(elsa =>
{
    elsa.UseAgents(agents =>
    {
        agents.Services.AddPluginProvider<WeatherPluginProvider>();
    });
});
```

## API Reference

### Core Interfaces

- `IAgentDefinition`: Code-first agent definition
- `IAgentWorkflowDefinition`: Code-first workflow definition
- `IKernelConfigProvider`: Provides agent configuration
- `IAgentServiceProvider`: AI service provider integration

### Extension Methods

- `AddAgentDefinition<T>()`: Register agent definition
- `AddAgentWorkflowDefinition<T>()`: Register workflow definition
- `AddPluginProvider<T>()`: Register plugin provider
- `AddAgentServiceProvider<T>()`: Register service provider

## Troubleshooting

### Agent Not Appearing in Workflow Designer

Ensure:
1. Agent is properly registered
2. AgentActivitiesFeature is enabled
3. Configuration is valid
4. Activity registry has been refreshed

### Tool Calling Not Working

Check:
1. Plugins are registered
2. Service provider supports function calling
3. Execution settings enable FunctionChoiceBehavior

### Multi-Agent Workflow Issues

Verify:
1. All referenced agents exist
2. Termination strategy is configured
3. Selection strategy is appropriate for workflow type

## Contributing

See the main repository CONTRIBUTING.md for contribution guidelines.

## License

This module is part of the Elsa Workflows project and follows the same license.

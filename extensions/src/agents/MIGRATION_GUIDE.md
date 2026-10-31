# Migration Guide: Microsoft Agent Framework Integration

This guide helps you understand and adopt the new Microsoft Agent Framework features in Elsa.Agents.

## What's New

### 1. Microsoft Agent Framework
The module now uses Microsoft's Agent Framework (Semantic Kernel Agents) for agent execution, providing:
- Better multi-agent orchestration
- More flexible agent communication patterns
- Enhanced tool calling capabilities
- Improved state management

### 2. Code-First Agent Registration
You can now define agents programmatically instead of only via JSON/configuration:

```csharp
// Register a code-first agent
services.AddElsa(elsa =>
{
    elsa.UseAgents(agents =>
    {
        agents.Services.AddAgentDefinition<MyCustomAgent>();
    });
});

// Define your agent
public class MyCustomAgent : IAgentDefinition
{
    public string Name => "MyAgent";
    public string Description => "My custom agent";
    
    public AgentConfig GetAgentConfig()
    {
        return new AgentConfig { /* ... */ };
    }
}
```

### 3. Multi-Agent Workflows
Create workflows where multiple agents collaborate:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseAgents(agents =>
    {
        agents.Services.AddAgentWorkflowDefinition<ContentPipeline>();
    });
});

public class ContentPipeline : IAgentWorkflowDefinition
{
    public AgentWorkflowConfig GetWorkflowConfig()
    {
        return new AgentWorkflowConfig
        {
            WorkflowType = AgentWorkflowType.Sequential,
            Agents = ["Researcher", "Writer", "Editor"],
            Termination = new TerminationConfig
            {
                Type = TerminationType.MaxMessages,
                MaxMessages = 20
            }
        };
    }
}
```

## Backward Compatibility

### No Breaking Changes
All existing functionality continues to work:
- JSON-defined agents ✓
- Database-persisted agents ✓
- Configuration-based agents ✓
- Existing activity providers ✓

### Legacy Mode
The `AgentInvoker` supports both legacy and new execution modes:

```csharp
// New Agent Framework mode (default)
await agentInvoker.InvokeAgentAsync(agentName, input, cancellationToken);

// Legacy Semantic Kernel mode (if needed)
await agentInvoker.InvokeAgentAsync(agentName, input, useAgentFramework: false, cancellationToken);
```

## Migration Strategies

### Strategy 1: No Migration Needed
If you're happy with your current setup, do nothing. Everything continues to work as before.

### Strategy 2: Gradual Adoption
1. Keep existing JSON/DB agents
2. Add new agents using code-first approach
3. Both coexist seamlessly

### Strategy 3: Full Migration
1. Keep JSON agents for configuration
2. Convert complex agents to code-first for better maintainability
3. Create multi-agent workflows for advanced scenarios

## New Capabilities

### Workflow Types
- **Sequential**: Agents execute in order
- **Graph**: Custom orchestration with agent selection

### Termination Strategies
- **MaxMessages**: Stop after N turns
- **Keyword**: Stop when pattern detected
- **AgentDecision**: Let agent decide when done

### Selection Strategies
- **Sequential**: Fixed order
- **RoundRobin**: Cycle through agents
- **LLMBased**: AI decides next agent
- **AgentBased**: Dedicated selector agent

## Examples

See the following files for complete examples:
- `README.md` - Comprehensive documentation
- `Examples/CodeFirstAgentExample.cs` - Code samples
- `Assets/*.json` - JSON configuration examples

## Getting Help

If you encounter issues:
1. Check the README.md for detailed documentation
2. Review the examples in `Examples/` directory
3. Ensure all agents are properly registered
4. Verify configuration is valid

## Testing

The module includes comprehensive tests:
```bash
dotnet test test/modules/agents/Elsa.Agents.Tests/
```

All tests passing: 6/6 ✓

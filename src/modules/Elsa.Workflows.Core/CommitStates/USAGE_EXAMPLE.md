# Commit Strategies - Usage Example

## Setting a Global Default Commit Strategy

You can configure an optional default workflow commit strategy globally by passing a strategy instance to `WithDefaultWorkflowCommitStrategy`.

### Basic Usage

```csharp
using Elsa.Workflows.CommitStates.Strategies;

services.AddElsa(elsa => elsa
    .UseWorkflows(workflows => workflows
        .WithDefaultWorkflowCommitStrategy(new ActivityExecutedWorkflowStrategy())
    )
);
```

**Benefits:**

- **Simplicity**: Pass any strategy instance directly
- **Auto-Registration**: The strategy is automatically added to the registry if not already present
- **No Manual Setup Required**: You don't need to call `UseCommitStrategies()` or `AddStandardStrategies()` first

### Configuration Within UseCommitStrategies

You can also configure the default strategy within the `UseCommitStrategies` callback:

```csharp
services.AddElsa(elsa => elsa
    .UseWorkflows(workflows => workflows
        .UseCommitStrategies(commitStrategies => 
        {
            // Set default strategy
            commitStrategies.SetDefaultWorkflowCommitStrategy(new WorkflowExecutingWorkflowStrategy());
        })
    )
);
```

## How It Works

The commit strategy resolution follows this priority:

1. **Workflow-specific strategy** (if set on the workflow via `Workflow.Options.CommitStrategyName`)
2. **Global default strategy** (if configured via `WithDefaultWorkflowCommitStrategy`)
3. **No automatic commit** (if neither is set)

## Available Standard Strategies

- `WorkflowExecutingWorkflowStrategy` - Commit before workflow execution
- `WorkflowExecutedWorkflowStrategy` - Commit after workflow execution
- `ActivityExecutingWorkflowStrategy` - Commit before each activity execution
- `ActivityExecutedWorkflowStrategy` - Commit after each activity execution
- `PeriodicWorkflowStrategy` - Commit periodically

## Examples

### Simple Configuration

```csharp
services.AddElsa(elsa => elsa
    .UseWorkflows(workflows => workflows
        .WithDefaultWorkflowCommitStrategy(new ActivityExecutedWorkflowStrategy())
    )
);
```

### With Additional Custom Strategies

```csharp
services.AddElsa(elsa => elsa
    .UseWorkflows(workflows => workflows
        .UseCommitStrategies(commitStrategies => 
        {
            // Add standard strategies
            commitStrategies.AddStandardStrategies();
            
            // Add custom strategy
            commitStrategies.Add(new MyCustomWorkflowStrategy());
            
            // Set default
            commitStrategies.SetDefaultWorkflowCommitStrategy(new ActivityExecutedWorkflowStrategy());
        })
    )
);
```

### With Custom Strategy Configuration

Since the method accepts instances, you can pass strategies with custom configuration:

```csharp
var customStrategy = new PeriodicWorkflowStrategy
{
    // Configure your strategy
};

services.AddElsa(elsa => elsa
    .UseWorkflows(workflows => workflows
        .WithDefaultWorkflowCommitStrategy(customStrategy)
    )
);
```

## Benefits

- **Consistency**: Set a default behavior for all workflows without configuring each individually
- **Flexibility**: Individual workflows can override the default by setting their own commit strategy
- **Simplicity**: Reduce boilerplate configuration across your workflows
- **Auto-Registration**: No need to remember to add strategies to the registry first
- **Instance-Based**: Pass configured strategy instances with custom settings

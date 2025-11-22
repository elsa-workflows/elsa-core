# Fluent Workflow Builder API Examples

This document demonstrates the usage of the fluent workflow builder API for creating code-first workflows in Elsa 3.

## Overview

The fluent API is a thin façade over the existing Elsa 3 workflow model. It provides:
- **Expressive syntax** for workflow definition
- **IntelliSense support** for better discoverability
- **Reduced boilerplate** compared to direct activity instantiation
- **Full compatibility** with the underlying WorkflowDefinition/activity graph

## Basic Usage

### Simple Sequential Workflow

```csharp
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;

// Get the workflow builder factory from DI
var workflowBuilderFactory = serviceProvider.GetRequiredService<IWorkflowBuilderFactory>();
var builder = workflowBuilderFactory.CreateBuilder();

// Build a simple sequential workflow
var workflow = await builder
    .WithName("HelloWorld")
    .WithVersion(1)
    .StartWith<WriteLine>(w => w.Text = new("Hello"))
    .Then<WriteLine>(w => w.Text = new("World"))
    .BuildAsync();
```

### Working with Variables

```csharp
var workflow = await builder
    .WithName("VariableDemo")
    .WithVariable("Counter", 0)
    .WithVariable("Message", "Hello")
    .StartWith<WriteLine>(w => w.Text = new("Starting"))
    .SetVar("Counter", 10)
    .SetVar("Message", "Counter is now 10")
    .Log("{{ Variables.Message }}")
    .BuildAsync();
```

### Setting Activity Names

```csharp
var workflow = await builder
    .WithName("NamedActivities")
    .StartWith<WriteLine>(w => w.Text = new("Step 1"))
    .Named("First Step")
    .Then<WriteLine>(w => w.Text = new("Step 2"))
    .Named("Second Step")
    .BuildAsync();
```

## Control Flow

### If Statement

```csharp
var workflow = await builder
    .WithName("ConditionalFlow")
    .WithVariable("Age", 25)
    .StartWith<WriteLine>(w => w.Text = new("Checking age"))
    .If("Variables.Age >= 18",
        then: b => b
            .Log("Adult")
            .Then<WriteLine>(w => w.Text = new("Access granted")),
        @else: b => b
            .Log("Minor")
            .Then<WriteLine>(w => w.Text = new("Access denied")))
    .BuildAsync();
```

### While Loop

```csharp
var workflow = await builder
    .WithName("WhileLoop")
    .WithVariable("Counter", 0)
    .StartWith<WriteLine>(w => w.Text = new("Starting loop"))
    .While("Variables.Counter < 5",
        body: b => b
            .Log("Counter: {{ Variables.Counter }}")
            .SetVar("Counter", "Variables.Counter + 1"))
    .BuildAsync();
```

### ForEach Loop

```csharp
var workflow = await builder
    .WithName("ForEachLoop")
    .WithVariable("Items", new List<string> { "A", "B", "C" })
    .StartWith<WriteLine>(w => w.Text = new("Processing items"))
    .ForEach<string>("Variables.Items",
        body: b => b
            .Log("Processing item: {{ CurrentValue }}"))
    .BuildAsync();
```

### Switch Statement

```csharp
var workflow = await builder
    .WithName("SwitchDemo")
    .WithVariable("Status", "active")
    .StartWith<WriteLine>(w => w.Text = new("Checking status"))
    .Switch(
        new Dictionary<string, (string condition, Action<IActivityBuilder> action)>
        {
            ["active"] = ("Variables.Status == 'active'", 
                b => b.Log("Status is active")),
            ["inactive"] = ("Variables.Status == 'inactive'", 
                b => b.Log("Status is inactive")),
            ["pending"] = ("Variables.Status == 'pending'", 
                b => b.Log("Status is pending"))
        },
        @default: b => b.Log("Unknown status"))
    .BuildAsync();
```

### Parallel Execution

```csharp
var workflow = await builder
    .WithName("ParallelDemo")
    .StartWith<WriteLine>(w => w.Text = new("Starting parallel tasks"))
    .Parallel(
        b => b.Log("Task 1").Then<WriteLine>(w => w.Text = new("Task 1 done")),
        b => b.Log("Task 2").Then<WriteLine>(w => w.Text = new("Task 2 done")),
        b => b.Log("Task 3").Then<WriteLine>(w => w.Text = new("Task 3 done")))
    .Log("All tasks completed")
    .BuildAsync();
```

## Nested Control Flow

```csharp
var workflow = await builder
    .WithName("NestedControlFlow")
    .WithVariable("X", 5)
    .WithVariable("Y", 10)
    .StartWith<WriteLine>(w => w.Text = new("Start"))
    .If("Variables.X > 0",
        then: b => b
            .Log("X is positive")
            .If("Variables.Y > 0",
                then: nested => nested.Log("Both positive"),
                @else: nested => nested.Log("X positive, Y negative")),
        @else: b => b
            .Log("X is not positive"))
    .BuildAsync();
```

## Complex Example

Here's a more complete example demonstrating multiple features:

```csharp
var workflow = await builder
    .WithName("OrderProcessing")
    .WithVersion(1)
    .WithId("order-processing-v1")
    .WithVariable("OrderId", 0)
    .WithVariable("OrderAmount", 0m)
    .WithVariable("IsApproved", false)
    .WithVariable("ProcessingResult", "")
    
    // Log start
    .StartWith<WriteLine>(w => w.Text = new("Starting order processing"))
    .Named("Start")
    
    // Set initial values
    .SetVar("OrderId", 12345)
    .SetVar("OrderAmount", 1500m)
    
    // Log order info
    .Log("Processing order {{ Variables.OrderId }} for amount {{ Variables.OrderAmount }}")
    
    // Check if order needs approval
    .If("Variables.OrderAmount > 1000",
        then: approval => approval
            .Log("Order requires manager approval")
            .Named("High Value Check")
            .SetVar("IsApproved", true)  // Simulate approval
            .Log("Manager approved"),
        @else: auto => auto
            .Log("Order auto-approved"))
    
    // Process if approved
    .If("Variables.IsApproved",
        then: process => process
            .Log("Processing approved order")
            .SetVar("ProcessingResult", "Success"),
        @else: reject => reject
            .Log("Order rejected")
            .SetVar("ProcessingResult", "Rejected"))
    
    // Log completion
    .Log("Order processing complete with result: {{ Variables.ProcessingResult }}")
    .BuildAsync();
```

## Extension Points

### Custom Activity Extensions

The fluent API is designed to be extensible. You can create your own extension methods for custom activities:

```csharp
public static class MyCustomActivityExtensions
{
    public static IActivityBuilder MyCustomActivity(
        this IActivityBuilder builder,
        string parameter)
    {
        return builder.Then<MyCustomActivity>(activity => 
        {
            activity.Parameter = new Input<string>(parameter);
        });
    }
}

// Usage:
var workflow = await builder
    .StartWith<WriteLine>(w => w.Text = new("Before custom"))
    .MyCustomActivity("some value")
    .Log("After custom")
    .BuildAsync();
```

### HTTP Activities (Requires Elsa.Http Module)

While HTTP activities are not included in the core module, you can create extension methods for them:

```csharp
// In your Elsa.Http extension assembly:
public static class HttpActivityBuilderExtensions
{
    public static IActivityBuilder HttpGet(
        this IActivityBuilder builder,
        string url)
    {
        return builder.Then<SendHttpRequest>(activity => 
        {
            activity.Url = new Input<Uri?>(new Uri(url));
            activity.Method = new Input<string>("GET");
        });
    }
    
    public static IActivityBuilder HttpPost(
        this IActivityBuilder builder,
        string url,
        object? body = null)
    {
        return builder.Then<SendHttpRequest>(activity => 
        {
            activity.Url = new Input<Uri?>(new Uri(url));
            activity.Method = new Input<string>("POST");
            if (body != null)
                activity.Content = new Input<object?>(body);
        });
    }
}
```

## Key Principles

1. **Thin Façade**: The fluent API is built on top of the existing Elsa 3 model and generates the same workflow structure.

2. **No Separate DSL**: All fluent API calls translate directly to the standard activity graph with connections.

3. **Full Compatibility**: Workflows built with the fluent API are identical to those built manually and can be serialized, stored, and executed in the same way.

4. **Extensible**: You can create extension methods for any custom activities or modules.

5. **IntelliSense-Friendly**: The API is designed to provide good discoverability through IDE auto-completion.

## Implementation Notes

### Core Interfaces

- `IActivityBuilder`: Main interface for chaining activities
- `IWorkflowBuilder`: Existing interface extended with fluent methods

### Core Classes

- `ActivityBuilder`: Main implementation for activity chaining
- `NestedActivityBuilder`: Handles nested activities (branches, loops)

### Extension Method Categories

- `WorkflowBuilderFluentExtensions`: Workflow-level operations
- `ActivityBuilderExtensions`: Basic activity operations
- `ControlFlowActivityBuilderExtensions`: Control flow structures
- `HttpActivityBuilderExtensions`: HTTP placeholder methods
- `WorkflowActivityBuilderExtensions`: Workflow-related placeholder methods

### Sequence Generation

When you chain activities using `.Then()`, they are automatically wrapped in a `Sequence` activity to ensure sequential execution. This is transparent to the user and matches Elsa's execution model.

## See Also

- [Elsa Workflows Documentation](https://docs.elsaworkflows.io/)
- [Activity Model](https://docs.elsaworkflows.io/docs/activities)
- [Workflow Builder](https://docs.elsaworkflows.io/docs/workflow-builder)

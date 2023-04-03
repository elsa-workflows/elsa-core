# Elsa Workflows

<a href="https://elsa-workflows.github.io/elsa-core/tree/v3">
  <p align="center">
    <img src="./artwork/android-elsa-portrait.png" alt="Elsa">
  </p>
</a>

[![Nuget](https://img.shields.io/nuget/v/elsa)](https://www.nuget.org/packages/Elsa/)
[![Build status](https://github.com/elsa-workflows/elsa-core/actions/workflows/ci.yml/badge.svg?branch=v3)](https://github.com/elsa-workflows/elsa-core/actions/workflows/ci.yml)
[![Discord](https://img.shields.io/discord/814605913783795763?label=chat&logo=discord)](https://discord.gg/hhChk5H472)
[![Stack Overflow questions](https://img.shields.io/badge/stackoverflow-elsa_workflows-orange.svg)]( http://stackoverflow.com/questions/tagged/elsa-workflows )
[![Subreddit subscribers](https://img.shields.io/reddit/subreddit-subscribers/elsaworkflows?style=social)](https://www.reddit.com/r/elsaworkflows/)

Elsa is a workflows library that enables workflow execution in any .NET application. Workflows can be defined in a variety of ways:

- Using C# code
- Using a designer
- Using JSON
- Using a custom DSL

## Documentation
Please checkout the [documentation website](https://v3.elsaworkflows.io/) to get started.

## Features

Here are some of the more important features offered by Elsa:

- Execute workflows in any .NET app.
- Supports both short-running and long-running workflows.
- Programming model loosely inspired on WF4 (composable activities).
- Support for complex activities such as Sequence, Flowchart and custom composite activities (which are like mini-workflows that can be used as activities).
- Ships with a workflows designer web component (currently supports Flowchart diagrams only).

## Console Examples

### Hello World

The following is a simple Hello World workflow created as a console application. The workflow is created using C#.

```csharp
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup service container.
var services = new ServiceCollection();

// Add Elsa services.
services.AddElsa();

// Build service container.
var serviceProvider = services.BuildServiceProvider();

// Create a workflow.
var workflow = new WriteLine("Hello World!");

// Resolve a workflow runner to run the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// Run the workflow.
await workflowRunner.RunAsync(workflow);
```

Outputs:

```shell
Hello World!
```

### Sequential workflows

To build workflows that execute more than one step, choose an activity that can do so. For example, the `Sequence` activity lets us add multiple activities to execute in sequence (plumbing code left out for brevity):

```csharp
// Create a workflow.
var workflow = new Sequence
{
    Activities =
    {
        new WriteLine("Hello World!"), 
        new WriteLine("Goodbye cruel world...")
    }
};
```

Outputs:

```shell
Hello World!
Goodbye cruel world...
```

### Conditions

The following demonstrates a workflow where it asks the user to enter their age, and based on this, offers a beer or a soda:

```csharp
// Declare a workflow variable for use in the workflow.
var ageVariable = new Variable<string>();

// Declare a workflow.
var workflow = new Sequence
{
    // Register the variable.
    Variables = { ageVariable }, 
    
    // Setup the sequence of activities to run.
    Activities =
    {
        new WriteLine("Please tell me your age:"), 
        new ReadLine(ageVariable), // Stores user input into the provided variable.,
        new If
        {
            // If aged 18 or up, beer is provided, soda otherwise.
            Condition = new Input<bool>(context => ageVariable.Get<int>(context) < 18),
            Then = new WriteLine("Enjoy your soda!"),
            Else = new WriteLine("Enjoy your beer!")
        },
        new WriteLine("Come again!")
    }
};
```

Notice that:

- To capture activity output, a workflow variable (ageVariable) is used.
- Depending on the result of the condition of the `If` activity, either the `Then` or the `Else` activity is executed.
- After the If activity completes, the final WriteLine activity is executed.

 ## ASP.NET Examples
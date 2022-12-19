# Elsa Workflows

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
- Programming model loosely inspired on WF4 (composable activities)
- Support for complex activities such as Sequence and Flowchart.
- Ships with a workflows designer web component (currently supports Flowchart diagrams only).

## Examples

### Hello World: Console
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
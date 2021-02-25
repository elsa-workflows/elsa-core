<p align="center">
  <img src="./doc/elsa-logo.png" alt="Elsa Logo">
</p>

## Elsa Workflows

[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Elsa)](https://www.nuget.org/packages/Elsa/2.0.0-preview5-0)
[![MyGet (with prereleases)](https://img.shields.io/myget/elsa-2/vpre/Elsa?label=myget)](https://www.myget.org/gallery/elsa-2)
[![Build status](https://ci.appveyor.com/api/projects/status/github/elsa-workflows/elsa-core?svg=true&branch=feature/elsa-2.0)](https://ci.appveyor.com/project/sfmskywalker/elsa)
[![Gitter](https://badges.gitter.im/elsa-workflows/community.svg)](https://gitter.im/elsa-workflows/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
[![Stack Overflow questions](https://img.shields.io/badge/stackoverflow-elsa_workflows-orange.svg)]( http://stackoverflow.com/questions/tagged/elsa-workflows )
![Docker Pulls](https://img.shields.io/docker/pulls/elsaworkflows/elsa-dashboard?label=elsa%20dashboard%3Adocker%20pulls)

Elsa Core is a workflows library that enables workflow execution in any .NET Core application.
Workflows can be defined not only using code but also as JSON, YAML or XML.

<p align="center">
  <img src="./doc/elsa-2-dashboard-plus-designer.gif" alt="Elsa 2 Preview">
</p>

## Get Started

Follow the [Getting Started](https://elsa-workflows.github.io/elsa-core/docs/installing-elsa-core) instructions on the [Elsa Workflows documentation site](https://elsa-workflows.github.io/elsa-core).

## Roadmap

Version 1.0

- [x] Workflow Invoker
- [x] Long-running Workflows
- [x] Workflows as code
- [x] Workflows as data
- [x] Correlation
- [x] Persistence: CosmosDB, Entity Framework Core, MongoDB, YesSQL 
- [x] HTML5 Workflow Designer Web Component
- [x] ASP.NET Core Workflow Dashboard
- [x] JavaScript Expressions
- [x] Liquid Expressions
- [x] Primitive Activities
- [X] Control Flow Activities
- [x] Workflow Activities
- [x] Timer Activities
- [x] HTTP Activities
- [x] Email Activities

Version 2.0

- [x] Composite Activities API
- [x] Service Bus Messaging
- [x] Workflow Host REST API
- [x] Workflow Server
- [x] Distributed Hosting Support (support for multi-node environments)
- [ ] Lucene Indexing
- [ ] New Workflow Designer + Dashboard
- [ ] Generic Command & Event Activities
- [ ] Job Activities (simplify kicking off a background process while the workflow sleeps & gets resumed once job finishes)

Version 3.0
- [ ] Composite Activity Definitions (with designer support)
- [ ] Localization Support
- [ ] State Machines
- [ ] Sagas


## Workflow Designer

Workflows can be visually designed using the Elsa Designer, a reusable & extensible HTML5 web component built with [StencilJS](https://stenciljs.com/).
To manage workflow definitions and instances, Elsa comes with a reusable Razor Class Library that provides a dashboard application in the form of an MVC area that you can include in your own ASP.NET Core application.

## Programmatic Workflows

Workflows can be created programmatically and then executed using `IWorkflowRunner` or scheduled for execution using `IWorkflowQueue`.

### Hello World
The following code snippet demonstrates creating a workflow with two WriteLine activities from code and then invoking it:

```c#

// Define a strongly-typed workflow.
public class HelloWorldWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder
            .WriteLine("Hello World!")
            .WriteLine("Goodbye cruel world...");
    }
}

// Setup a service collection.
var services = new ServiceCollection()
    .AddElsa()
    .AddConsoleActivities()
    .AddWorkflows<HelloWorldWorkflow>()
    .BuildServiceProvider();

// Run startup actions (not needed when registering Elsa with a Host).
var startupRunner = services.GetRequiredService<IStartupRunner>();
await startupRunner.StartupAsync();

// Get a workflow runner.
var workflowRunner = services.GetService<IWorkflowRunner>();

// Run the workflow.
await workflowRunner.RunWorkflowAsync<HelloWorld>();

// Output:
// /> Hello World!
// /> Goodbye cruel world...
```

## Declarative Workflows

Instead of writing C# code to define a workflow, Elsa also supports reading and writing declarative workflows from the database as well as from JSON formats.
The following is a small example that constructs a workflow using a generic set of workflow and activity models, describing the workflow.
This models is then serialized to JSON and deserialized back into the model

```csharp
// Create a service container with Elsa services.
var services = new ServiceCollection()
    .AddElsa()


    // For production use.
    .UseYesSqlPersistence()
    
    // Or use any of the other supported persistence providers such as EF Core or MongoDB:
    // .UseEntityFrameworkPersistence(ef => ef.UseSqlite())
    // .UseMongoDbPersistence()

    .BuildServiceProvider();

// Run startup actions (not needed when registering Elsa with a Host).
var startupRunner = services.GetRequiredService<IStartupRunner>();
await startupRunner.StartupAsync();

// Define a workflow.
var workflowDefinition = new WorkflowDefinition
{
    WorkflowDefinitionId = "SampleWorkflow",
    WorkflowDefinitionVersionId = "1", 
    Version = 1,
    IsPublished = true,
    IsLatest = true,
    IsEnabled = true,
    PersistenceBehavior = WorkflowPersistenceBehavior.Suspended,
    Activities = new[]
    {
        new ActivityDefinition
        {
            ActivityId = "activity-1",
            Type = nameof(WriteLine),
            Properties = new ActivityDefinitionProperties
            {
                [nameof(WriteLine.Text)] = new ActivityDefinitionPropertyValue
                {
                    Syntax = "Literal",
                    Expression = "Hello World!",
                    Type = typeof(string)
                }
            }
        }, 
    }
};

// Serialize workflow definition to JSON.
var serializer = services.GetRequiredService<IContentSerializer>();
var json = serializer.Serialize(workflowDefinition);

Console.WriteLine(json);

// Deserialize workflow definition from JSON.
var deserializedWorkflowDefinition = serializer.Deserialize<WorkflowDefinition>(json);

// Materialize workflow.
var materializer = services.GetRequiredService<IWorkflowBlueprintMaterializer>();
var workflowBlueprint = materializer.CreateWorkflowBlueprint(deserializedWorkflowDefinition);

// Execute workflow.
var workflowRunner = services.GetRequiredService<IWorkflowRunner>();
await workflowRunner.RunWorkflowAsync(workflowBlueprint);
```

## Persistence

Elsa abstractes away data access, which means you can use any persistence provider you prefer. 

## Long Running Workflows

Elsa has native support for long-running workflows. As soon as a workflow is halted because of some blocking activity, the workflow is persisted.
When the appropriate event occurs, the workflow is loaded from the store and resumed. 

## Why Elsa Workflows?

One of the main goals of Elsa is to **enable workflows in any .NET application** with **minimum effort** and **maximum extensibility**.
This means that it should be easy to integrate workflow capabilities into your own application.

### What about Azure Logic Apps?

As powerful and as complete Azure Logic Apps is, it's available only as a managed service in Azure. Elsa on the other hand allows you to host it not only on Azure, but on any cloud provider that supports .NET Core. And of course you can host it on-premise.

Although you can implement long-running workflows with Logic Apps, you would typically do so with splitting your workflow with multiple Logic Apps where one workflow invokes the other. This can make the logic flow a bit hard to follow.
with Elsa, you simply add triggers anywhere in the workflow, making it easier to have a complete view of your application logic. And if you want, you can still invoke other workflows form one workflow.

### What about Windows Workflow Foundation?

I've always liked Windows Workflow Foundation, but unfortunately [development appears to have halted](https://forums.dotnetfoundation.org/t/what-is-the-roadmap-of-workflow-foundation/3066).
Although there's an effort being made to [port WF to .NET Standard](https://github.com/dmetzgar/corewf), there are a few reasons I prefer Elsa:

- Elsa intrinsically supports triggering events that starts new workflows and resumes halted workflow instances in an easy to use manner. E.g. `workflowHost.TriggerWorkflowAsync("HttpRequestTrigger");"` will start and resume all workflows that either start with or are halted on the `HttpRequestTrigger`. 
- Elsa has a web-based workflow designer. I once worked on a project for a customer that was building a huge SaaS platform. One of the requirements was to provide a workflow engine and a web-based editor. Although there are commercial workflow libraries and editors out there, the business model required open-source software. We used WF and the re-hosted Workflow Designer. It worked, but it wasn't great.

### What about Orchard Workflows?

Both [Orchard](http://docs.orchardproject.net/en/latest/Documentation/Workflows/) and [Orchard Core](https://orchardcore.readthedocs.io/en/dev/docs/reference/modules/Workflows/) ship with a powerful workflows module, and both are awesome.
In fact, Elsa Workflows is taken & adapted from Orchard Core's Workflows module. Elsa uses a similar model, but there are some differences:  

- Elsa Workflows is completely decoupled from web, whereas Orchard Core Workflows is coupled to not only the web, but also the Orchard Core Framework itself.
- Elsa Workflows can execute in any .NET Core application without taking a dependency on any Orchard Core packages.

## Features

TODO

## How to use Elsa

TODO

### Setting up a Workflow Designer ASP.NET Core Application

TODO: describe all the steps to add packages and register services.

### Setting up a Workflow Host .NET Application 

TODO: describe all the steps to add packages and register services.

### Building & Running Elsa Workflows Dashboard

TODO

# Code of Conduct

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct). 

### .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).

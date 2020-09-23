## Elsa Workflows

[![Nuget](https://img.shields.io/nuget/v/Elsa)](https://www.nuget.org/packages/Elsa/)
[![MyGet (with prereleases)](https://img.shields.io/myget/elsa/vpre/Elsa.Core.svg?label=myget)](https://www.myget.org/gallery/elsa)
[![Build status](https://ci.appveyor.com/api/projects/status/rqg10opfpy78yiga/branch/develop?svg=true)](https://ci.appveyor.com/project/sfmskywalker/elsa-preview/branch/develop)
[![Gitter](https://badges.gitter.im/elsa-workflows/community.svg)](https://gitter.im/elsa-workflows/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
[![Stack Overflow questions](https://img.shields.io/badge/stackoverflow-elsa_workflows-orange.svg)]( http://stackoverflow.com/questions/tagged/elsa-workflows )
![Docker Pulls](https://img.shields.io/docker/pulls/elsaworkflows/elsa-dashboard?label=elsa%20dashboard%3Adocker%20pulls)

Elsa Core is a workflows library that enables workflow execution in any .NET Core application.
Workflows can be defined not only using code but also as JSON, YAML or XML.

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

- [ ] Service Bus Messaging
- [ ] Generic Command & Event Activities
- [ ] Workflow Host REST API
- [ ] Workflow Host gRPC API
- [ ] Workflow Server
- [ ] Activity Harvesting
- [ ] Distributed Hosting Support (support for multi-node environments)
- [ ] Localization Support
- [ ] More activities
- [ ] Workflow Designer UI improvements
- [ ] Activity Editor UI improvements

Version 3.0

- [ ] State Machines
- [ ] Container Activities

## Workflow Designer

Workflows can be visually designed using [Elsa Designer](https://github.com/elsa-workflows/elsa-designer-html), a reusable & extensible HTML5 web component built with [StencilJS](https://stenciljs.com/).
To manage workflow definitions and instances, Elsa comes with a reusable Razor Class Library that provides a dashboard application in the form of an MVC area that you can include in your own ASP.NET Core application.

![Web-based workflow designer](/doc/dashboard-sample-1.png)

## Programmatic Workflows

Workflows can be created programmatically and then executed using `IWorkflowInvoker`.

### Hello World
The following code snippet demonstrates creating a workflow with two custom activities from code and then invoking it:

```c#

// Define a strongly-typed workflow.
public class HelloWorldWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder
            .StartWith<HelloWorld>()
            .Then<GoodByeWorld>();
    }
}

// Setup a service collection.
var services = new ServiceCollection()
    .AddWorkflows()
    .AddActivity<HelloWorld>()
    .AddActivity<GoodByeWorld>()
    .BuildServiceProvider();

// Invoke the workflow.
var invoker = services.GetService<IWorkflowInvoker>();
await invoker.InvokeAsync<HelloWorldWorkflow>();

// Output:
// /> Hello World!
// /> Goodbye cruel World...
```

### Persistence

Workflows can be persisted using virtually any storage mechanism.
The following providers will be supported:

- In Memory
- File System
- SQL Server
- MongoDB
- CosmosDB

### Formats

Currently, workflows can be stored in YAML or JSON format.
The following demonstrates a simple workflow expressed in YAML and JSON, respectively:

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

The following lists some of Elsa's key features:

- **Small, simple and fast**. The library should be lean & mean, meaning that it should be **easy to use**, **fast to execute** and **easy to extend** with custom activities. 
- Invoke arbitrary workflows as if they were **functions of my application**.
- Trigger events that cause the appropriate workflows to **automatically start/resume** based on that event.
- Support **long-running workflows**. When a workflow executes and encounters an activity that requires e.g. user input, the workflow will halt, be persisted and go out of memory until it's time to resume. this could be a few seconds later, a few minutes, hours, days or even years.
- **Correlate** workflows with application-specific data. This is a key requirement for long-running workflows.
- Store workflows in a **file-based** format so I can make it part of source-control.
- Store workflows in a **database** when I don't want to make them part of source control.
- A **web-based designer**. Whether I store my workflows on a file system or in a database, and whether I host the designer online or only on my local machine, I need to be able to edit my workflows.
- Configure workflow activities with **expressions**. Oftentimes, information being processed by a workflow is dynamic in nature, and activities need a way to interact with this information. Workflow expressions allow for this.
- **Extensible** with application-specific **activities**, **custom stores** and **scripting engines**.
- Invoke other workflows. This allows for invoking reusable application logic from various workflows. Like invoking general-purpose functions from C# without having to duplicate code.
- **View & analyze** executed workflow instances. I want to see **which path** a workflow took, its **runtime state**, where it **faulted** and **compensate** faulted workflows.
- **Embed** the web-based workflow designer in **my own dashboard** application. This gives me the option of creating a single Workflow Host that runs all of my application logic, but also the option of hosting a workflows runtime in individual micro services (allowing for orchestration as well as choreography).
- **Separation of concerns**: The workflow core library, runtime and designer should all be separated. I.e. when the workflow host should not have a dependency on the web-based designer. This allows one for example to implement a desktop-based designer, or not use a designer at all and just go with YAML files. The host in the end only needs the workflow definitions and access to persistence stores.
- **On premise** or **managed** in the cloud - both scenarios are supported, because Elsa is just a set of NuGet packages that you reference from your application.

## How to use Elsa

Elsa is distributed as a set of NuGet packages, which makes it easy to add to your application.
When working with Elsa, you'll typically want to have at least two applications:

1. An ASP.NET Core application to host the workflows designer.
2. A .NET application that executed workflows

### Setting up a Workflow Designer ASP.NET Core Application

TODO: describe all the steps to add packages and register services.

### Setting up a Workflow Host .NET Application 

TODO: describe all the steps to add packages and register services.

### Building & Running Elsa Workflows Dashboard

In order to build & run Elsa on your local machine, follow these steps:

1. Clone the repository.
2. Run NPM install on `src\dashboard\Elsa.Dashboard\Theme\argon-dashboard`
3. Execute `gulp build` from the directory `src\dashboard\Elsa.Dashboard\Theme\argon-dashboard`
4. Open a shell and navigate to `src\dashboard\Elsa.Dashboard.Web` and run `dotnet run`.
5. Navigate to http://localhost:22174/elsa/home

# Code of Conduct

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct). 

### .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).

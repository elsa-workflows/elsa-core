![Web-based workflow designer](/doc/elsa-cover.png)

## Elsa Workflows

Elsa Workflows is a set of lean & mean workflow components that you can use in your .NET Core applications.
With Elsa, you can invoke and trigger workflows from your own application. Workflows can be expressed as JSON, YAML, XML or in code.

You can manually handcraft workflows or use the web-based workflow designer.

![Web-based workflow designer](/doc/workflow-sample-2.png)

## Why Elsa Workflows?

One of the key reasons for Elsa's existence is to **enable workflows in any .NET application** with **minimum effort** and **maximum extensibility**.

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

Both [Orchard](http://docs.orchardproject.net/en/latest/Documentation/Workflows/) and [Orchard Core](https://orchardcore.readthedocs.io/en/dev/OrchardCore.Modules/OrchardCore.Workflows/) ship with a powerful workflows module, and both are awesome.
In fact, Elsa Workflows is taken & adapted from Orchard Core's Workflows module. Elsa uses a similar model, but there are some technical differences:  

- Elsa Workflows is completely decoupled from web, whereas Orchard Core Workflows is coupled to not only the web, but also the Orchard Core Framework itself.
- Elsa Workflows can execute in any .NET Core application without taking a dependency on any Orchard Core packages (not to be confused with Elsa Workflows Designer, which takes advantage of some Orchard Core packages).
- Elsa Workflows separates activity models from activity execution logic.

I am a huge fan of Orchard Core, and its Workflows module is one of its biggest gems. In fact, the Elsa Workflows web-based designer depends on Orchard Core Framework packages because Orchard Core is that useful!
An important roadmap item is to provide an Orchard Core module called `OrchardCore.ElsaWorkfows`, which uses Elsa's engine and web-based designer within the context of an Orchard Core application and provides Orchard Core-specific activities such as content-related triggers and actions.

As mentioned earlier: this is one of the main reasons that Elsa exists: to enable workflows in any .NET application. Orchard Core included.
There are a few reasons I think contributing to `OrchardCore.ElsaWorkflows` makes sense:

- Elsa potentially has a broader audience, because workflows are applicable in more environments than Orchard Core.
- Orchard Core is awesome, and `OrchardCore.Workflows` is a key feature of it. If Elsa is used more widely, it is likely to also have more community support, which means more features.  

## Features

The following lists some of Elsa's key features:

- **Small, simple and fast**. The library should be lean & mean, meaning that it should be **easy to use**, **fast to execute** and **easy to extend** with custom activities.
- It must be a set of **libraries**. This allows me to create my application anyway I like, and implement workflow capabilities as I see fit. Thanks to ASP.NET Core's application model however, creating a workflow designer & workflow host is as simple as referencing the right packages and making a few calls. 
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

> Although you can separate the workflow designer from the workflow host, you're free to host both in one and the same ASP.NET Core application.

### Setting up a Workflow Designer ASP.NET Core Application

TODO: describe all the steps to add packages and register services.

### Setting up a Workflow Host .NET Application 

TODO: describe all the steps to add packages and register services.

## Building & Running Elsa Sourcecode

Although Elsa is distributed as NuGet Packages for you to reference from your own .NET applications, when contributing, troubleshooting or see how things work, you'll want to clone the repository and get that up & running.
Follow below steps to do just thar.

### Running Elsa Workflows Dashboard

In order to run Elsa on your local machine, follow these steps:

1. Clone the repository.
2. Run NPM install on all folders containing packages.json (or run `node npm-install.js` - a script in the root that recursively installs the Node packages)
3. Open a shell and navigate to `src/samples/SampleDashboard.Web` and run `dotnet run`.
4. Navigate to https://localhost:44397/

## Running Elsa Workflows Host

(TODO)

## Roadmap

(TODO)
 
- Describe all the features (core engine, runtime, webbased designer, YAML, scripting, separation of designer from invoker from engine).
- Describe various use cases.
- Describe how to use.
- Describe architecture.
- Describe how to implement (custom host, custom dashboard).
- Implement more activities (fork, join, script, HTTP request, loops, etc.)
- Implement integration with Orchard Core (separate repo)
- Detailed documentation
- Open API Activity Harvester
- Automatic UI for Activity Editor


## Elsa Workflows

Elsa Workflows is a set of lean & mean workflow components that you can use in your .NET Core applications.
It's inspired on [Orchard Core Workflows](https://github.com/OrchardCMS/OrchardCore), [Azure Logic Apps](https://azure.microsoft.com/services/logic-apps/) and [Windows Workflow Foundation](https://docs.microsoft.com/en-us/previous-versions/dotnet/articles/dd851337(v=msdn.10)), but with some key differentiators.

With Elsa, you can invoke and trigger workflows from your own application. Workflows can be expressed as JSON, YAML, XML or in code.

Although you can manually handcraft workflows, a much easier way is to use the web-based workflow designer that is provided as a separate package.

![Web-based workflow designer](/doc/workflow-sample-1.png)

## Why Elsa Workflows?

The reason I started this project is because I need a .NET Core workflows engine that meet the following requirements:

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

### Elsa vs Azure Logic Apps

As powerful and as complete Azure Logic Apps is, it's available only as a managed service in Azure. Elsa on the other hand allows you to host it not only in Azure, but on any cloud provider that supports .NET Core. And of course you can host it on-premise.

Although you can implement long-running workflows with Logic Apps, you would typically do so with splitting your workflow with multiple Logic Apps where one workflow invokes the other. This can make the logic flow a bit hard to follow.
with Elsa, you simply add triggers anywhere in the workflow, making it easier to have a complete view of your application logic. And if you want, you can still invoke other workflows form one workflow.

### Elsa vs Windows Workflow Foundation

I've always been a big fan of Windows Workflow Foundation, but unfortunately [development appears to have halted](https://forums.dotnetfoundation.org/t/what-is-the-roadmap-of-workflow-foundation/3066).
Although there's an effort being made to [port WF to .NET Standard](https://github.com/dmetzgar/corewf), there are a few reasons I prefer Elsa:

- Elsa intrinsically supports triggering events that starts new workflows and resumes halted workflow instances in an easy to use manner. E.g. `workflowHost.TriggerWorkflowAsync("HttpRequestTrigger");"` will start and resume all workflows that either start with or are halted on the `HttpRequestTrigger`. 
- Elsa has a web-based workflow designer. I once worked on a project for a customer that was building a huge SaaS platform. One of the requirements was to provide a workflow engine and a web-based editor. Although there are commercial workflow libraries and editors out there, the business model required open-source software. We used WF and the re-hosted Workflow Designer. It worked, but it wasn't great.

## Running Elsa Workflows Dashboard

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



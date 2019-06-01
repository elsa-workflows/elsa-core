![Web-based workflow designer](/doc/elsa-cover.png)

## Elsa Workflows

Elsa is a visual programming tool that allows you to implement parts of your application with workflows as well to implement your entire application using workflows.
It is implemented as a set NuGet packages that you can use in your .NET Core applications.
With Elsa, you can invoke and trigger workflows from your own application. Workflows can be expressed as JSON, YAML, XML or in code.

You can manually handcraft workflows or use the web-based workflow designer.

![Web-based workflow designer](/doc/workflow-sample-3.png)

## Programmatic Workflows

Workflows can be created programmatically and then executed using `IWorkflowInvoker`.

### Hello World
The following code snippet demonstrates creating workflow from code and then invoking it:

```c#
// Setup a service collection.
var services = new ServiceCollection()
    .AddWorkflowsInvoker()
    .AddConsoleActivities()
    .BuildServiceProvider();

// Create a workflow invoker.
var invoker = services.GetService<IWorkflowInvoker>();

// Create a workflow.
var workflow = new Workflow();

// Add a single activity to execute.
workflow.Activities.Add(new WriteLine("Hello World!"));

// Invoke the workflow.
invoker.InvokeAsync(workflow);

// Output: Hello World!
```

### Calculator
The following code snippet demonstrates setting up a workflow programmatically using the `WorkflowBuilder`.
```c#
private static Workflow CreateSampleWorkflow()
{
    // 1. Ask two numbers
    // 2. Ask operation to apply to the two numbers.
    // 3. Show the result of the calculation.
    // 4. Ask user to try again or exit program.

    return new WorkflowBuilder()
        .Add(new WriteLine("Please enter a number:") { Alias = "Start" })
        .Next(new ReadLine("a"))
        .Next(new WriteLine("Please enter another number:"))
        .Next(new ReadLine("b"))
        .Next(new WriteLine("Please enter the operation you would like to perform on the two numbers. Allowed operations:\r\n-Add\r\n-Subtract\r\n-Divide\r\n-Multiply"))
        .Next(new ReadLine("op"))
        .Next(new Switch(JavaScriptEvaluator.SyntaxName, "op"), @switch =>
        {
            @switch.Next(new SetVariable("result", JavaScriptEvaluator.SyntaxName, "parseFloat(a) + parseFloat(b)"), "Add").Next("ShowResult");
            @switch.Next(new SetVariable("result", JavaScriptEvaluator.SyntaxName, "a - b"), "Subtract").Next("ShowResult");
            @switch.Next(new SetVariable("result", JavaScriptEvaluator.SyntaxName, "a * b"), "Multiply").Next("ShowResult");
            @switch.Next(new SetVariable("result", JavaScriptEvaluator.SyntaxName, "a / b"), "Divide").Next("ShowResult");
        })
        .Next(new WriteLine(new WorkflowExpression<string>(JavaScriptEvaluator.SyntaxName, "`Result: ${result}`")){ Alias = "ShowResult"})
        .Next(new WriteLine("Try again? (Y/N)"))
        .Next(new ReadLine("retry"))
        .Next(new IfElse(new WorkflowExpression<bool>(JavaScriptEvaluator.SyntaxName, "retry.toLowerCase() === 'y'")), ifElse =>
        {
            ifElse.Next("Start", "True");
            ifElse.Next(new WriteLine("Bye!"), "False");
        })
        .BuildWorkflow();
}
```

### Persistence

Workflows can be persisted using virtually any storage mechanism.
Out of the box come the following providers:

- In Memory
- File System
- SQL Server
- MongoDB
- CosmosDB

Although there are no structural differences between a workflow definition and a workflow instance, Elsa supports storing workflow definitions in store separate from workflow instances.
This enables scenarios where for example you want to store workflow definition files as part of source control, but leverage a high-performing SQL Server to read and write workflow instances.

The following code snippet demonstrates writing and reading workflow definitions and instances using the file system storage provider.

```c#
// Setup configuration for the file system stores.
var rootDir = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "workflows");
var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
    {
        ["Workflows:Directory"] = rootDir,
        ["Workflows:Format"] = YamlTokenFormatter.FormatName
    })
    .Build();

// Setup a service collection and use the FileSystemProvider for both workflow definitions and workflow instances.
var services = new ServiceCollection()
    .AddWorkflowsInvoker()
    .AddConsoleActivities()
    .AddFileSystemWorkflowDefinitionStoreProvider(configuration.GetSection("Workflows"))
    .AddFileSystemWorkflowInstanceStoreProvider(configuration.GetSection("Workflows"))
    .AddSingleton(Console.In)
    .BuildServiceProvider();

// Define a workflow.
var workflowDefinition = new WorkflowBuilder()
    .Add(new WriteLine("Hi! What's your name?"))
    .Next(new ReadLine("name"))
    .Next(new WriteLine(new WorkflowExpression<string>(JavaScriptEvaluator.SyntaxName, "`Nice to meet you, ${name}!`")))
    .BuildWorkflow();

// Save the workflow definition using IWorkflowDefinitionStore.
var workflowDefinitionStore = services.GetService<IWorkflowDefinitionStore>();
await workflowDefinitionStore.SaveAsync(workflowDefinition, CancellationToken.None);

// Load the workflow definition.
workflowDefinition = await workflowDefinitionStore.GetByIdAsync(workflowDefinition.Id, CancellationToken.None);

// Invoke the workflow.
var invoker = services.GetService<IWorkflowInvoker>();
var workflowExecutionContext = await invoker.InvokeAsync(workflowDefinition, workflowDefinition.Activities.First());

// Save the workflow instance using IWorkflowInstanceStore.
var workflowInstance = workflowExecutionContext.Workflow;
var workflowInstanceStore = services.GetService<IWorkflowInstanceStore>();
await workflowInstanceStore.SaveAsync(workflowInstance, CancellationToken.None);
```

> NOTE: Although the above example demonstrates storing a workflow instance, you don't normally need to do this yourself. When invoking `AddFileSystemWorkflowInstanceStoreProvider` or `AddFileSystemWorkflowDefinitionStoreProvider`, an event handler is registered as well that listens for certain events raised by the workflow invoker.
> When a workflow is finished executing, an event is raised in response to which the workflow instance is persisted. In a future update, persistence of workflow instances will be configurable on per workflow definition. You may not want to persist all workflow instances. Alternatively, you may want to configure a retention policy that discards or archives older workflow instances that have finished.  

### Formats

Currently, workflows can be stored in YAML or JSON format.
The following demonstrates a simple workflow expressed in YAML and JSON, respectively:

**YAML**
```yaml
activities:
- name: WriteLine
  id: hi-activity
  textExpression:  
    syntax: PlainText
    expression: Hi! What's your name?
- name: ReadLine
  id: read-name-activity
  argumentName: name
- name: WriteLine
  id: greeting-activity
  textExpression:
    syntax: JavaScript
    expression: '`Nice to meet you, ${name}!`'
connections:
- source:
    activityId: hi-activity
    name: Done
  target:
    activityId: read-name-activity
- source:
    activityId: read-name-activity
    name: Done
  target:
    activityId: greeting-activity
```

**JSON**
```json
{
  "activities": [
    {
      "name": "WriteLine",
      "id": "hi-activity",
      "textExpression": {
        "syntax": "PlainText",
        "expression": "Hi! What's your name?"
      }
    },
    {
      "id": "read-name-activity",
      "name": "ReadLine",
      "argumentName": "name"
    },
    {
      "name": "WriteLine",
      "id": "greeting-activity",
      "textExpression": {
        "syntax": "JavaScript",
        "expression": "`Nice to meet you, ${name}!`"
      }
    }
  ],
  "connections": [
    {
      "source": {
        "activityId": "hi-activity",
        "name": "Done"
      },
      "target": {
        "activityId": "read-name-activity"
      }
    },
    {
      "source": {
        "activityId": "read-name-activity",
        "name": "Done"
      },
      "target": {
        "activityId": "greeting-activity"
      }
    }
  ]
}
```

The following demonstrates how to parse a workflow in YAML format:

```c#
// Setup a service collection and use the FileSystemProvider for both workflow definitions and workflow instances.
var services = new ServiceCollection()
    .AddWorkflowsInvoker()
    .AddConsoleActivities()
    .AddSingleton(Console.In)
    .BuildServiceProvider();

// Load the data and specify data format.
var data = Resources.SampleWorkflowDefinition;
var format = YamlTokenFormatter.FormatName; // "YAML"

// Deserialize the workflow from data.
var serializer = services.GetService<IWorkflowSerializer>();
var workflowDefinition = await serializer.DeserializeAsync(data, format, CancellationToken.None);

// Invoke the workflow.
var invoker = services.GetService<IWorkflowInvoker>();
await invoker.InvokeAsync(workflowDefinition, workflowDefinition.Activities.First());
```

## Workflow Host
In addition to programmatically invoke specific workflows using `IWorkflowInvoker`, you can instead "trigger" workflows using `IWorkflowHost`.
For example, if you have a bunch of workflows defined that start with e.g. a `HttpRequestTrigger` activity, you can execute all of these workflows using the following statement:

`await workflowHost.TriggerWorkflowsAsync("HttpRequestTrigger", Variables.Empty, cancellationToken)` 

What this will do is invoke every workflow that either starts with a _HttpRequestTrigger_ activity or is blocked on said activity.

## Long Running Workflows

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

Both [Orchard](http://docs.orchardproject.net/en/latest/Documentation/Workflows/) and [Orchard Core](https://orchardcore.readthedocs.io/en/dev/OrchardCore.Modules/OrchardCore.Workflows/) ship with a powerful workflows module, and both are awesome.
In fact, Elsa Workflows is taken & adapted from Orchard Core's Workflows module. Elsa uses a similar model, but there are some differences:  

- Elsa Workflows is completely decoupled from web, whereas Orchard Core Workflows is coupled to not only the web, but also the Orchard Core Framework itself.
- Elsa Workflows can execute in any .NET Core application without taking a dependency on any Orchard Core packages (not to be confused with Elsa Workflows Designer, which takes advantage of some Orchard Core packages).
- Elsa Workflows separates activity models from activity execution logic.

I am a big fan of Orchard Core, and its Workflows module is in my opinion one of its biggest gems. In fact, the Elsa Workflows web-based designer depends on Orchard Core Framework packages because Orchard Core is that useful!
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

### Setting up a Workflow Designer ASP.NET Core Application

TODO: describe all the steps to add packages and register services.

### Setting up a Workflow Host .NET Application 

TODO: describe all the steps to add packages and register services.

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
- Implement more activities
- Implement integration with Orchard Core (separate repo)
- Detailed documentation
- Open API Activity Harvester
- MassTransit Activity Harvester
- RabbitMQ Activities
- Azure Service Bus Activities
- Automatic UI for Activity Editor


# Elsa Workflows

<a href="https://v3.elsaworkflows.io/">
  <p align="center">
    <img src="./design/artwork/android-elsa-portrait.png" alt="Elsa">
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

## Documentation
Please checkout the [documentation website](https://v3.elsaworkflows.io/) to get started.

## Known Issues and Limitations

- Documentation is still a work in progress
- The designer is not yet fully embeddable in other applications. This is planned for a future release
- C# and Python expressions are not yet fully tested
- Bulk Dispatch Workflows is a new activity and not yet fully tested
- Input / Output is not yet implemented in the Workflow Instance Viewer
- Starting workflows from the designer is only supported for workflows that do not require input and do not start with a trigger. This is planned for a future release.
- The designer currently only supports Flowchart activities. Support for Sequence and StateMachine activities is planned for a future release. 

## Features

Here are some of the more important features offered by Elsa:

- Execute workflows in any .NET app with support as of .NET 6 and beyond.
- Supports both short-running and long-running workflows.
- Programming model loosely inspired on Windows Workflow Foundation.
- Advanced web-based drag & drop designer with support for custom activities.
- Activity model natively supports composition. Examples are activities such as `Sequence`, `Flowchart` and `ForEach`.
- Parallel execution of activities.
- Built-in activities for common scenarios such as sending emails, making HTTP calls, scheduling tasks, sending and receiving messages, etc.
- Workflow versioning.
- Workflow migration via API.
- Easy integration with external applications via HTTP, message queues, etc.
- Actor model for increased workflow throughput.
- Dynamic expressions with support for C#, JavaScript, Python and Liquid.
- Persistence agnostic. Supports Entity Framework Core, MongoDB and Dapper out of the box.
- [Elsa Studio](https://github.com/elsa-workflows/elsa-studio), a modular Blazor dashboard app to managed & design workflows.

## Roadmap

The following features are planned for future releases:

- [ ] Multi-tenancy
- [ ] State Machine activity
- [ ] Designer support for Sequence activity & StateMachine activity
- [ ] BPMN 2.0 support
- [ ] DMN support
- [ ] Workflow migration to new versions via UI
- [ ] Capsules ("hot" deployable workflow packages)

## Use cases

Elsa can be used in a variety of scenarios. Here are some examples:

- Long-running workflows such as order fulfillment, product approval, etc.
- Short-running workflows such as sending emails, generating PDFs, etc.
- Scheduled workflows such as sending a daily report, etc.
- Event-driven workflows such as sending a welcome email when a user signs up, etc.

## Console Example

Let's take a look at a simple example that demonstrates how to create a workflow and run it. The following example is a console application that creates a workflow that writes "Hello World!" to the console.

### Hello World

The following is a simple Hello World workflow created as a console application. 

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

Notice that in the above example, we executed the `WriteLine` activity directly.

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

## ASP.NET Example

When working with workflows that involve timers, messages and other events, running a simple Console application is not enough.
In this case, we need a proper host that can run the workflows in the background and handle events.

ASP.NET Core is a great host for this purpose. The following example demonstrates how to create a simple ASP.NET Core application that acts as a workflow server.

```csharp
using Elsa.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add Elsa services.
services.AddElsa(elsa => elsa
    
    // Add workflows from this program.
    .AddWorkflowsFrom<Program>()
        
    // Enable Elsa HTTP module for HTTP related activities. 
    .UseHttp()
);

// Configure ASP.NET's middleware pipeline.
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// Add Elsa HTTP middleware to handle requests mapped to HTTP Endpoint activities.
app.UseWorkflows();

// Start accepting requests.
app.Run();
```

The above example demonstrates how to:

- Add workflows from the current program.
- Enable the HTTP module to handle HTTP related activities.

### HTTP Endpoint

The following example demonstrates how to create a workflow that handles HTTP requests:

```csharp
public class HelloWorldHttpWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new HttpEndpoint
                {
                    Path = new("/hello-world"),
                    SupportedMethods = new(new[] { HttpMethods.Get }),
                    CanStartWorkflow = true
                },
                new WriteHttpResponse
                {
                    StatusCode = new(HttpStatusCode.OK),
                    Content = new("Hello world!")
                }
            }
        };
    }
}
```

The above example demonstrates how to:

- Create an HTTP endpoint that listens for GET requests on the `/hello-world` path.
- Respond to the request with a `200 OK` status code and a `Hello world!` message.
- The `CanStartWorkflow` property is set to `true` to indicate that this endpoint can start a workflow.
- The `HttpEndpoint` activity is followed by a `WriteHttpResponse` activity that writes the response to the client.

### Timer

The following example demonstrates how to create a workflow that executes every 5 seconds:

```csharp
public class HeartbeatWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new Timer(TimeSpan.FromSeconds(5))
                {
                    CanStartWorkflow = true
                },
                new WriteLine(context => $"Heartbeat at {context.GetRequiredService<ISystemClock>().UtcNow}"),
            }
        };
    }
}
```

The above example demonstrates how to:

- Create a timer that executes every 5 seconds.
- The `CanStartWorkflow` property is set to `true` to indicate that this timer can start a workflow.
- The `Timer` activity is followed by a `WriteLine` activity that writes the current time to the console.
- The `ISystemClock` service is used to get the current time.
- The `context` parameter is used to access the service.

## Elsa Server + Elsa Studio

So far, we have seen a simple Console and ASP.NET Core application that runs workflows. However, these applications do not provide a way to design workflows.
For this, we need the following:

- Elsa Server: The ASP.NET Core application needs to expose API endpoints that can be used to design workflows.
- Elsa Studio: A Blazor application that can be used to design workflows.

To setup a simple Elsa Server application, follow these steps:

1. Create a new ASP.NET Core application.
2. Add the necessary packages
3. Make the necessary changes in Program.cs

Let's go through the above steps in detail.

### Create Elsa Server

Create a new ASP.NET Core application using the following command:

```shell
dotnet new web -n "ElsaServer" -f net8.0
cd ElsaServer
dotnet add package Elsa --prerelease
dotnet add package Elsa.EntityFrameworkCore --prerelease
dotnet add package Elsa.Identity --prerelease
dotnet add package Elsa.Scheduling --prerelease
dotnet add package Elsa.Workflows.Api --prerelease
```

Next, open Program.cs file and replace its contents with the following code:

```csharp
builder.Services.AddElsa(elsa =>
{
    // Configure Management layer to use EF Core.
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore());

    // Configure Runtime layer to use EF Core.
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore());
    
    // Default Identity features for authentication/authorization.
    elsa.UseIdentity(identity =>
    {
        identity.TokenOptions = options => options.SigningKey = "secret signing key for tokens";
        identity.UseAdminUserProvider();
    });
    
    // Configure ASP.NET authentication/authorization.
    elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());
    
    // Expose Elsa API endpoints.
    elsa.UseWorkflowsApi();
    
    // Setup a SignalR hub for real-time updates from the server.
    els.UseRealTimeWorkflows();
    
    // Enable C# workflow expressions
    elsa.UseCSharp();
    
    // Enable HTTP activities.
    elsa.UseHttp();
    
    // Use timer activities.
    elsa.UseScheduling();
    
    // Register custom activities from the application, if any.
    elsa.AddActivitiesFrom<Program>();
    
    // Register custom workflows from the application, if any.
    elsa.AddWorkflowsFrom<Program>();
});

// Configure CORS to allow designer app hosted on a different origin to invoke the APIs.
builder.Services.AddCors(cors => cors
    .AddDefaultPolicy(policy => policy
        .AllowAnyOrigin() // For demo purposes only. Use a specific origin instead.
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("x-elsa-workflow-instance-id"))); // Required for Elsa Studio in order to support running workflows from the designer. Alternatively, you can use the `*` wildcard to expose all headers.

// Add Health Checks.
builder.Services.AddHealthChecks();

// Configure ASP.NET's middleware pipeline.
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi(); // Use Elsa API endpoints.
app.UseWorkflows(); // Use Elsa middleware to handle HTTP requests mapped to HTTP Endpoint activities.
app.UseWorkflowsSignalRHubs(); // Optional SignalR integration. Elsa Studio uses SignalR to receive real-time updates from the server. 

app.Run();
```

### Create Elsa Studio

Create a new Blazor WebAssembly application using the following command:

```shell
dotnet new blazorwasm-empty -n "ElsaStudio" -f net8.0
cd ElsaStudio
dotnet add package Elsa.Studio --prerelease
dotnet add package Elsa.Studio.Core.BlazorWasm --prerelease
dotnet add package Elsa.Studio.Login.BlazorWasm --prerelease
```

Next, open Program.cs file and replace its contents with the following code:

```csharp
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Login.BlazorWasm.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Build the host.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

// Register root components.
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.RegisterCustomElsaStudioElements();

// Register shell services and modules.
builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddRemoteBackend(configureElsaClientBuilderOptions: elsaClient => elsaClient.ConfigureHttpClientBuilder = httpClientBuilder => httpClientBuilder.AddHttpMessageHandler<AuthenticatingApiHttpMessageHandler>());
builder.Services.AddLoginModule();
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();

// Build the application.
var app = builder.Build();

// Run each startup task.
var startupTaskRunner = app.Services.GetRequiredService<IStartupTaskRunner>();
await startupTaskRunner.RunStartupTasksAsync();

// Run the application.
await app.RunAsync();
```

For a cleaner project structure, eliminate the following directories and files:

- wwwroot/css

Within the wwwroot directory, create a new appsettings.json file and populate it with the subsequent content:

```json
{
  "Backend": {
    "Url": "https://localhost:5001/elsa/api"
  }
}
```

Finally, open the wwwroot/index.html file and replace its content with the code showcased below:

```html
<!DOCTYPE html>
<html>

<head>
    <meta charset="utf-8"/>
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"/>
    <title>MyApplication</title>
    <base href="/"/>
    <link rel="apple-touch-icon" sizes="180x180" href="_content/Elsa.Studio.Shell/apple-touch-icon.png">
    <link rel="icon" type="image/png" sizes="32x32" href="_content/Elsa.Studio.Shell/favicon-32x32.png">
    <link rel="icon" type="image/png" sizes="16x16" href="_content/Elsa.Studio.Shell/favicon-16x16.png">
    <link rel="manifest" href="_content/Elsa.Studio.Shell/site.webmanifest">
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="https://fonts.googleapis.com/css2?family=Ubuntu:wght@300;400;500;700&display=swap" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;500;600;700&display=swap" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Grandstander:wght@100&display=swap" rel="stylesheet">
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link href="_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.css" rel="stylesheet" />
    <link href="_content/Radzen.Blazor/css/material-base.css" rel="stylesheet" >
    <link href="_content/Elsa.Studio.Shell/css/shell.css" rel="stylesheet">
    <link href="Elsa.Studio.Host.Wasm.styles.css" rel="stylesheet">
</head>

<body>
<div id="app">
    <div class="loading-splash mud-container mud-container-maxwidth-false">
        <h5 class="mud-typography mud-typography-h5 mud-primary-text my-6">Loading...</h5>
    </div>
</div>

<div id="blazor-error-ui">
    An unhandled error has occurred.
    <a href="" class="reload">Reload</a>
    <a class="dismiss">🗙</a>
</div>
<script src="_content/BlazorMonaco/jsInterop.js"></script>
<script src="_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js"></script>
<script src="_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js"></script>
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
<script src="_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.js"></script>
<script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
<script src="_framework/blazor.webassembly.js"></script>
</body>

</html>
```

To see your application in action, execute the following command:

```shell
dotnet run
```

Your application should now be accessible at https://localhost:5001. The port number might vary based on your configuration. By default, you can log in using:

```
Username: admin
Password: password
```
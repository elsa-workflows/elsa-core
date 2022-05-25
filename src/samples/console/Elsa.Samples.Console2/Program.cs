using System;
using System.IO;
using System.Reflection;
using Elsa.Dsl.Extensions;
using Elsa.Dsl.Services;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.JavaScript.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var serviceProvider = CreateServices();
var typeSystem = serviceProvider.GetRequiredService<ITypeSystem>();
var dslEngine = serviceProvider.GetRequiredService<IDslEngine>();
var functionActivityRegistry = serviceProvider.GetRequiredService<IFunctionActivityRegistry>();

// Map .NET types to Elsa DSL. Optionally using an alias. 
typeSystem.Register<int>("int");
typeSystem.Register<float>("float");
typeSystem.Register<string>("string");
typeSystem.Register("Variable<>", typeof(Variable<>));
typeSystem.Register<Sequence>();
typeSystem.Register<ReadLine>();
typeSystem.Register<WriteLine>();
typeSystem.Register<HttpEndpoint>();
typeSystem.Register<Timer>();

// Map functions to activities.
functionActivityRegistry.RegisterFunction("print", nameof(WriteLine), new[] { nameof(WriteLine.Text) });
functionActivityRegistry.RegisterFunction("read", nameof(ReadLine), new[] { nameof(ReadLine.Result) });

var assembly = Assembly.GetExecutingAssembly();

// Read text file containing Elsa DSL.
var scriptName = "Demo4";
var resource = assembly.GetManifestResourceStream($"Elsa.Samples.Console2.{scriptName}.elsa");
var script = await new StreamReader(resource!).ReadToEndAsync();

// Evaluate the DSL. The result will be an executable workflow definition. 
var workflowDefinition = dslEngine.Parse(script);

// Run the workflow.
var workflowEngine = serviceProvider.GetRequiredService<IWorkflowRunner>();
await workflowEngine.RunAsync(workflowDefinition);

IServiceProvider CreateServices()
{
    var services = new ServiceCollection();

    services
        .AddElsa()
        .AddDsl()
        .AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning))
        .AddJavaScriptExpressions();

    return services.BuildServiceProvider();
}
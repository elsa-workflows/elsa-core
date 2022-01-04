using System;
using System.IO;
using System.Reflection;
using Elsa.Activities.Console;
using Elsa.Activities.Http;
using Elsa.Activities.Timers;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Dsl.Abstractions;
using Elsa.Dsl.Contracts;
using Elsa.Dsl.Extensions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.InMemory.Extensions;
using Elsa.Scripting.JavaScript;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var serviceProvider = CreateServices();
var typeSystem = serviceProvider.GetRequiredService<ITypeSystem>();
var dslEngine = serviceProvider.GetRequiredService<IDslEngine>();
var functionActivityRegistry = serviceProvider.GetRequiredService<IFunctionActivityRegistry>();

typeSystem.Register<int>("int");
typeSystem.Register<float>("float");
typeSystem.Register<string>("string");
typeSystem.Register("Variable<>", typeof(Variable<>));
typeSystem.Register<Sequence>();
typeSystem.Register<ReadLine>();
typeSystem.Register<WriteLine>();
typeSystem.Register<HttpTrigger>();
typeSystem.Register<TimerTrigger>();

functionActivityRegistry.RegisterFunction("print", nameof(WriteLine), new[] { nameof(WriteLine.Text) });
functionActivityRegistry.RegisterFunction("read", nameof(ReadLine), new[] { nameof(ReadLine.Output) });

var assembly = Assembly.GetExecutingAssembly();
var resource = assembly.GetManifestResourceStream("Elsa.Samples.Console2.Sample1.elsa");

var script = await new StreamReader(resource!).ReadToEndAsync();
var workflowDefinition = dslEngine.Parse(script);

var workflowEngine = serviceProvider.GetRequiredService<IWorkflowEngine>();
await workflowEngine.ExecuteAsync(workflowDefinition);

IServiceProvider CreateServices()
{
    var services = new ServiceCollection();

    services
        .AddElsa()
        .AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning))
        .AddInMemoryPersistence()
        .AddJavaScriptExpressions();

    return services.BuildServiceProvider();
}
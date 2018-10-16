using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Expressions;
using Flowsharp.Handlers;
using Flowsharp.Persistence;
using Flowsharp.Persistence.InMemory;
using Flowsharp.Runtime;
using Flowsharp.Runtime.Abstractions;
using Flowsharp.Samples.Console.Handlers;
using Flowsharp.Samples.Console.Programs;
using Flowsharp.Serialization;
using Flowsharp.Serialization.Formatters;
using Flowsharp.Serialization.Tokenizers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowsharp.Samples.Console
{
    class Program
    {
        static async Task Main()
        {
            var services = new ServiceCollection()
                .AddSingleton<IExpressionEvaluator, JavaScriptEvaluator>()
                .AddSingleton<IExpressionEvaluator, PlainTextEvaluator>()
                .AddSingleton<IWorkflowExpressionEvaluator, WorkflowExpressionEvaluator>()
                .AddSingleton<IWorkflowInvoker, WorkflowInvoker>()
                .AddSingleton<IActivityInvoker, ActivityInvoker>()
                .AddSingleton<IWorkflowSerializer, WorkflowSerializer>()
                //.AddSingleton<ITokenFormatter, JsonTokenFormatter>()
                .AddSingleton<ITokenFormatter, YamlTokenFormatter>()
                .AddSingleton<IWorkflowTokenizer, WorkflowTokenizer>()
                .AddSingleton<ITokenizerInvoker, TokenizerInvoker>()
                .AddSingleton<ITokenizer, DefaultTokenizer>()
                .AddSingleton<ITokenizer, ActivityTokenizer>()
                .AddSingleton<IWorkflowHost, WorkflowHost>()
                .AddLogging(logging => logging.AddConsole());

            services
                .AddSingleton<IActivityHandler, SetVariableHandler>()
                .AddSingleton<IActivityHandler, IfElseHandler>()
                .AddSingleton<IActivityHandler, ReadLineHandler>()
                .AddSingleton<IActivityHandler, WriteLineHandler>();

            services
                .AddSingleton<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>()
                .AddSingleton<IWorkflowInstanceStore, InMemoryWorkflowInstanceStore>();

            services
                .AddSingleton<AdditionWorkflowProgram>()
                .AddSingleton<AdditionWorkflowProgramLongRunning>()
                .AddSingleton<FileBasedWorkflowProgramLongRunning>()
                .AddSingleton<WorkflowHostProgram>();

            var serviceProvider = services.BuildServiceProvider();
            //var program = serviceProvider.GetRequiredService<AdditionWorkflowProgramLongRunning>();
            //var program = serviceProvider.GetRequiredService<FileBasedWorkflowProgramLongRunning>();
            var program = serviceProvider.GetRequiredService<WorkflowHostProgram>();
            
            await program.RunAsync(CancellationToken.None);
        }
    }
}

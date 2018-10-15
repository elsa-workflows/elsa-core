using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Expressions;
using Flowsharp.Handlers;
using Flowsharp.Samples.Console.Handlers;
using Flowsharp.Samples.Console.Programs;
using Flowsharp.Serialization;
using Flowsharp.Services;
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
                .AddSingleton<IWorkflowSerializer, JsonWorkflowSerializer>()
                .AddLogging(logging => logging.AddConsole());

            services
                .AddSingleton<IActivityHandler, SetVariableHandler>()
                .AddSingleton<IActivityHandler, IfElseHandler>()
                .AddSingleton<IActivityHandler, ReadLineHandler>()
                .AddSingleton<IActivityHandler, WriteLineHandler>();

            services
                .AddSingleton<AdditionWorkflowProgram>()
                .AddSingleton<AdditionWorkflowProgramLongRunning>();

            var serviceProvider = services.BuildServiceProvider();
            var program = serviceProvider.GetRequiredService<AdditionWorkflowProgramLongRunning>();
            
            await program.RunAsync(CancellationToken.None);
        }
    }
}

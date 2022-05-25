using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Options;
using Elsa.Pipelines.ActivityExecution.Components;
using Elsa.Pipelines.WorkflowExecution.Components;
using Elsa.Samples.Console1.Workflows;
using Elsa.Services;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Samples.Console1;

class Program
{
    static async Task Main()
    {
        var serviceProvider = CreateServices();

        serviceProvider
            // Configure activity execution pipeline.
            .ConfigureDefaultActivityExecutionPipeline(pipeline => pipeline
                .UseLogging()
                .UseDefaultActivityInvoker()
            )

            // Configure workflow engine execution pipeline.
            .ConfigureDefaultWorkflowExecutionPipeline(pipeline => pipeline
                .UsePersistence()
                .UseStackBasedActivityScheduler()
            );

        var workflow1 = new Func<IActivity>(HelloWorldWorkflow.Create);
        var workflow2 = new Func<IActivity>(HelloGoodbyeWorkflow.Create);
        var workflow3 = new Func<IActivity>(GreetingWorkflow.Create);
        var workflow4 = new Func<IActivity>(ConditionalWorkflow.Create);
        var workflow5 = new Func<IActivity>(ForWorkflow.Create);
        var workflow6 = new Func<IActivity>(BlockingWorkflow.Create);
        var workflow7 = new Func<IActivity>(ForkedWorkflow.Create);
        var workflow8 = new Func<IActivity>(CustomizedActivityWorkflow.Create);
        var workflow9 = new Func<IActivity>(VariablesWorkflow.Create);
        var workflow10 = new Func<IActivity>(WhileWorkflow.Create);
        var workflow11 = new Func<IActivity>(ForEachWorkflow.Create);
        var workflow12 = new Func<IActivity>(ParallelForEachWorkflow.Create);
        var workflow13 = new Func<IActivity>(BlockingParallelForEachWorkflow.Create);
        var workflow14 = new Func<IActivity>(FlowchartWorkflow.Create);
        var workflow15 = new Func<IActivity>(BreakForWorkflow.Create);

        var workflowFactory = workflow2;
        var workflowGraph = workflowFactory();
        var workflow = Workflow.FromActivity(workflowGraph);

        var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();
        var workflowExecutionResult = await workflowRunner.RunAsync(workflow);
        var workflowState = workflowExecutionResult.WorkflowState;
        var bookmarks = new List<Bookmark>(workflowExecutionResult.Bookmarks);

        while (bookmarks.Any())
        {
            workflow.Root = workflowFactory();
            foreach (var bookmark in bookmarks.ToList())
            {
                Console.WriteLine("Press enter to resume workflow with bookmark {0}.", new
                {
                    bookmark.Id,
                    bookmark.Name,
                    bookmark.Hash,
                    bookmark.ActivityId,
                    bookmark.ActivityInstanceId,
                    bookmark.CallbackMethodName
                });

                Console.ReadLine();

                var resumeResult = await workflowRunner.RunAsync(workflow, workflowState, bookmark);
                workflowState = resumeResult.WorkflowState;
                bookmarks = resumeResult.Bookmarks.ToList();
            }
        }
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();

        services
            .AddElsa(elsa => elsa.Configure<ElsaRuntimeOptions>()
                .WithStandardOutStreamProvider(sp => new CustomOutStreamProvider(Console.Out)))
            .AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning));

        return services.BuildServiceProvider();
    }
}

public class CustomOutStreamProvider : IStandardOutStreamProvider
{
    private readonly TextWriter _textWriter;

    public CustomOutStreamProvider(TextWriter textWriter)
    {
        _textWriter = textWriter;
    }

    public TextWriter GetTextWriter() => _textWriter;
}
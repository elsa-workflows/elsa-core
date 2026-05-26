using ConsoleLogStreaming.Core.Options;
using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Features;

namespace Elsa.Diagnostics.ConsoleLogs.Features;

public class ConsoleLogsFeature(IModule module) : FeatureBase(module)
{
    public Action<ConsoleLogOptions>? ConfigureOptions { get; set; }

    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<ConsoleLogsFeature>();
        Module.Configure<WorkflowsFeature>(ConfigureWorkflowPipelines);
    }

    public override void Apply()
    {
        Services.AddConsoleLogsServices(ConfigureOptions);
        Module.AddFastEndpointsFromModule();
    }

    private static void ConfigureWorkflowPipelines(WorkflowsFeature workflows)
    {
        var workflowExecutionPipeline = workflows.WorkflowExecutionPipeline;
        var activityExecutionPipeline = workflows.ActivityExecutionPipeline;

        workflows.WorkflowExecutionPipeline = builder =>
        {
            builder.UseConsoleLogContext();
            workflowExecutionPipeline(builder);
        };

        workflows.ActivityExecutionPipeline = builder =>
        {
            builder.UseConsoleLogContext();
            activityExecutionPipeline(builder);
        };
    }
}

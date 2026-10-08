using Elsa.Extensions;
using Elsa.Workflows.Middleware.Activities;
using Elsa.Workflows.Middleware.Workflows;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Admission;

// This scoped host owns the actual compositions, rather than attempting to identify opaque
// caller-supplied factories. Existing host setup callbacks are replaced and never invoked.
// Commit handlers retain normal state/bookmark/variable persistence; heartbeat and dispatch
// outbox middleware are outside this isolated, synchronous execution host's supported contract.
internal sealed class AdmissionExecutionComposition
{
    private readonly Lazy<WorkflowExecutionPipeline> _workflow;
    private readonly Lazy<ActivityExecutionPipeline> _activity;

    public AdmissionExecutionComposition(IServiceProvider services)
    {
        // Publish this scoped attestation before resolving the scheduler's activity invoker,
        // whose constructor in turn resolves the activity pipeline from this same scope.
        _activity = new(() =>
        {
            var pipeline = new ActivityExecutionPipeline(services, builder => builder
                .UseLogging()
                .UseMiddleware<Elsa.Workflows.Middleware.Activities.ExceptionHandlingMiddleware>()
                .UseExecutionLogging()
                .UseNotifications()
                .UseDefaultActivityInvoker());
            pipeline.Freeze();
            return pipeline;
        });
        _workflow = new(() =>
        {
            var pipeline = new WorkflowExecutionPipeline(services, builder => builder
                .UseExecutionCycleTracking()
                .UsePersistentVariables()
                .UseMiddleware<Elsa.Workflows.Middleware.Workflows.ExceptionHandlingMiddleware>()
                .UseDefaultActivityScheduler());
            pipeline.Freeze();
            return pipeline;
        });
    }

    public WorkflowExecutionPipeline Workflow => _workflow.Value;
    public ActivityExecutionPipeline Activity => _activity.Value;

    public void Validate(IServiceProvider services)
    {
        if (!ReferenceEquals(Workflow, services.GetRequiredService<IWorkflowExecutionPipeline>()) ||
            !ReferenceEquals(Activity, services.GetRequiredService<IActivityExecutionPipeline>()))
        {
            throw new InvalidOperationException("The actual admission execution compositions are not the frozen host-owned instances.");
        }
    }
}

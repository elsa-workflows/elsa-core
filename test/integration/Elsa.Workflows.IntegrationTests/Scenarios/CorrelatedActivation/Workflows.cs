using Elsa.Expressions.JavaScript.Models;
using Elsa.Workflows.ActivationValidators;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Activities.SetOutput;
using Elsa.Workflows.Runtime.ActivationValidators;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.CorrelatedActivation;

public class CorrelatedSingletonConversationWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithActivationStrategyType<CorrelatedSingletonStrategy>();
        builder.Root = new Event("Hold");
    }
}

public class WaitForDeniedDispatchConversationWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new DispatchWorkflow
                {
                    WorkflowDefinitionId = new(nameof(CorrelatedSingletonConversationWorkflow)),
                    CorrelationId = new("denied-dispatch-correlation"),
                    WaitForCompletion = new(true)
                },
                new Finish()
            }
        };
    }
}

public class WaitForDeniedBulkDispatchConversationWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new BulkDispatchWorkflows
                {
                    WorkflowDefinitionId = new(nameof(CorrelatedSingletonConversationWorkflow)),
                    Items = new(new[] { "item-1" }),
                    CorrelationIdFunction = new(JavaScriptExpression.Create("`denied-bulk-dispatch-correlation`")),
                    WaitForCompletion = new(true),
                    ChildFaulted = new SetOutput
                    {
                        OutputName = new("ActivationDenied"),
                        OutputValue = new(true)
                    }
                },
                new Finish()
            }
        };
    }
}

public class SingletonConversationWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithActivationStrategyType<SingletonStrategy>();
        builder.Root = new Event("Hold");
    }
}

public class GroupedConversationWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Event("Hold");
    }
}

public class AllowAlwaysConversationWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithActivationStrategyType<AllowAlwaysStrategy>();
        builder.Root = new Event("Hold");
    }
}

public class OtherCorrelatedSingletonConversationWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithActivationStrategyType<CorrelatedSingletonStrategy>();
        builder.Root = new Event("Hold");
    }
}

public class GlobalCorrelationConversationWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithActivationStrategyType<CorrelationStrategy>();
        builder.Root = new Event("Hold");
    }
}

public class OtherGlobalCorrelationConversationWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithActivationStrategyType<CorrelationStrategy>();
        builder.Root = new Event("Hold");
    }
}

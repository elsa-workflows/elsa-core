using Elsa.Workflows.Activities;
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

public class GroupedConversationWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
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

using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Scheduling.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.IntegrationTests.Scenarios.WorkflowCancellation.Workflows;

public class BulkSuspendedWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        object[] items = Enumerable.Range(0,10000).Select(x => (object) x).ToArray();
        
        builder.Root = new Sequence
        {
            Activities =
            {
                new Start(),
                new Delay(TimeSpan.FromSeconds(10)),
                new BulkDispatchWorkflows
                {
                    WorkflowDefinitionId = new Input<string>(nameof(SimpleChildWorkflow)),
                    Items = new Input<ICollection<object>>(items)
                }
            },
        };
    }
}
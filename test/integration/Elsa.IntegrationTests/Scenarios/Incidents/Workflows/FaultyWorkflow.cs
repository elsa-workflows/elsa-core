using Elsa.IntegrationTests.Scenarios.Incidents.Statics;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.IntegrationTests.Scenarios.Incidents.Workflows;

public class FaultyWorkflow : WorkflowBase
{
    
    
    protected override void Build(IWorkflowBuilder builder)
    {
        var start = new WriteLine("Start");
        var step1A = new WriteLine("Step 1a");
        var event1 = new Event("Event 1");
        var step1B = new WriteLine("Step 1b");
        var step2A = new WriteLine("Step 2a");
        var event2 = new Event("Event 2");
        var step2B = new WriteLine("Step 2b");
        var fault = new Fault("Whoops!");

        builder.WorkflowOptions.IncidentStrategyType = TestSettings.IncidentStrategyType;
        
        builder.Root = new Flowchart
        {
            Activities =
            {
                start,
                step1A,
                event1,
                step1B,
                step2A,
                event2,
                step2B,
                fault
            },
            
            Connections = new[]
            {
                new Connection(start, step1A),
                new Connection(step1A, event1),
                new Connection(event1, step1B),
                
                new Connection(start, step2A),
                new Connection(step2A, fault),
                new Connection(fault, event2),
                new Connection(event2, step2B),
            }
        };
    }
}
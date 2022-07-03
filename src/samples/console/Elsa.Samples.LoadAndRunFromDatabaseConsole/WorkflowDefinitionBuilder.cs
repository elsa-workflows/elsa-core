using Elsa.Activities.Console;
using Elsa.Models;

namespace Elsa.Samples.LoadAndRunFromDatabaseConsole;

public static class WorkflowDefinitionBuilder
{
    public static WorkflowDefinition CreateDemoWorkflowDefinition() => new()
    {
        Id = "SampleWorkflow-Version-1",
        DefinitionId = "SampleWorkflow",
        Version = 1,
        IsPublished = true,
        IsLatest = true,
        PersistenceBehavior = WorkflowPersistenceBehavior.Suspended,
        Activities = new[]
        {
            new ActivityDefinition
            {
                ActivityId = "activity-1",
                Type = nameof(WriteLine),
                Properties = new List<ActivityDefinitionProperty>()
                {
                    ActivityDefinitionProperty.Liquid(nameof(WriteLine.Text), "Hello World")
                }
            },
        }
    };
}
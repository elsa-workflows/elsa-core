using System;
using Elsa.Expressions;
using Elsa.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Swagger
{
    public class WorkflowDefinitionExample : IExamplesProvider<WorkflowDefinition>
    {
        public WorkflowDefinition GetExamples()
        {
            return new WorkflowDefinition
            {
                Id = 1,
                Name = "ProcessOrderWorkflow",
                DisplayName = "Process Order Workflow",
                Description = "Process new orders",
                Version = 1,
                IsPublished = true,
                WorkflowDefinitionId = Guid.NewGuid().ToString("N"),
                WorkflowDefinitionVersionId = Guid.NewGuid().ToString("N"),
                ContextOptions = new WorkflowContextOptions
                {
                    ContextFidelity = WorkflowContextFidelity.Burst,
                    ContextType = typeof(string)
                },
                Activities = new[]
                {
                    new ActivityDefinition
                    {
                        ActivityId = "activity-1",
                        Description = "Write \"Hello\"",
                        Type = "WriteLine",
                        Name = "Activity1",
                        DisplayName = "Write \"Hello\"",
                        Properties = new ActivityDefinitionProperties
                        {
                            ["Text"] = new ActivityDefinitionPropertyValue("Hello", LiteralHandler.SyntaxName, typeof(string))
                        }
                    },
                    new ActivityDefinition
                    {
                        ActivityId = "activity-2",
                        Description = "Write \"World!\"",
                        Type = "WriteLine",
                        Name = "Activity2",
                        DisplayName = "Write \"World!\"",
                        Properties = new ActivityDefinitionProperties
                        {
                            ["Text"] = new ActivityDefinitionPropertyValue("World!", LiteralHandler.SyntaxName, typeof(string))
                        }
                    }
                },
                Connections = new[] {new ConnectionDefinition("activity-1", "activity-2", OutcomeNames.Done)}
            };
        }
    }
}
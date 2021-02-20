using System;
using System.Collections.Generic;
using Elsa.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Swagger.Examples
{
    public class WorkflowDefinitionExample : IExamplesProvider<WorkflowDefinition>
    {
        public WorkflowDefinition GetExamples()
        {
            return new()
            {
                Id = Guid.NewGuid().ToString("N"),
                DefinitionId = Guid.NewGuid().ToString("N"),
                Name = "ProcessOrderWorkflow",
                DisplayName = "Process Order Workflow",
                Description = "Process new orders",
                Version = 1,
                IsPublished = true,
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
                        Properties = new List<ActivityDefinitionProperty>()
                        {
                            ActivityDefinitionProperty.Literal("Text", "Hello")
                        }
                    },
                    new ActivityDefinition
                    {
                        ActivityId = "activity-2",
                        Description = "Write \"World!\"",
                        Type = "WriteLine",
                        Name = "Activity2",
                        DisplayName = "Write \"World!\"",
                        Properties = new List<ActivityDefinitionProperty>()
                        {
                            ActivityDefinitionProperty.Literal("Text", "World!")
                        }
                    }
                },
                Connections = new[] { new ConnectionDefinition("activity-1", "activity-2", OutcomeNames.Done) }
            };
        }
    }
}
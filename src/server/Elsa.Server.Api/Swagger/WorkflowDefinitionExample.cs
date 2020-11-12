using System.Collections.Generic;
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
                WorkflowDefinitionId = "26bd1dca39954c8f93364a18c2a35528",
                WorkflowDefinitionVersionId = "c8c57402e68949598a45411c74e463e1",
                Type = "Workflow",
                ContextOptions = new WorkflowContextOptions
                {
                    ContextFidelity = WorkflowContextFidelity.Burst,
                    ContextType = typeof(Order)
                },
                Activities = new[]
                {
                    new ActivityDefinition
                    {
                        ActivityId = "activity-1",
                    }
                }
            };
        }
    }

    public class Order
    {
    }
}
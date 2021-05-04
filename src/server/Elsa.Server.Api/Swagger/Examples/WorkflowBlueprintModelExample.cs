using Elsa.Models;
using Elsa.Server.Api.Endpoints.WorkflowRegistry;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Swagger.Examples
{
    public class WorkflowBlueprintModelExample : IExamplesProvider<WorkflowBlueprintModel>
    {
        public WorkflowBlueprintModel GetExamples()
        {
            return new()
            {
                Id = "my-workflow",
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
                    new ActivityBlueprintModel
                    {
                        Id = "activity-1",
                        Type = "WriteLine",
                        Name = "Activity1"
                    },
                    new ActivityBlueprintModel
                    {
                        Id = "activity-2",
                        Type = "WriteLine",
                        Name = "Activity2",
                    }
                },
                Connections = new[] { new ConnectionModel { SourceActivityId = "activity-1", TargetActivityId = "activity-2", Outcome = OutcomeNames.Done } }
            };
        }
    }
}
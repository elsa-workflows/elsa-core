using System;
using Elsa.Models;
using Elsa.Server.Api.Endpoints.WorkflowDefinitions;
using NodaTime;
using Parlot.Fluent;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Swagger.Examples
{
    public class WorkflowDefinitionSummaryModelExample : IExamplesProvider<WorkflowDefinitionSummaryModel>
    {
        public WorkflowDefinitionSummaryModel GetExamples() =>
            new(
                Guid.NewGuid().ToString("N"),
                Guid.NewGuid().ToString("N"),
                null,
                "ProcessOrderWorkflow",
                "Process Order Workflow",
                "Process new orders",
                1,
                false,
                WorkflowPersistenceBehavior.Suspended,
                true,
                false,
                new Variables(),
                SystemClock.Instance.GetCurrentInstant()
            );
    }
}
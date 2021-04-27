using System.Linq;
using Elsa.Server.Api.Endpoints.WorkflowDefinitions;
using Elsa.Server.Api.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Swagger.Examples
{
    public class WorkflowDefinitionSummaryModelPagedListExample : IExamplesProvider<PagedList<WorkflowDefinitionSummaryModel>>
    {
        public PagedList<WorkflowDefinitionSummaryModel> GetExamples()
        {
            var examples = Enumerable.Range(1, 3).Select(_ => new WorkflowDefinitionSummaryModelExample().GetExamples()).ToList();
            return new PagedList<WorkflowDefinitionSummaryModel>(examples, 0, 10, examples.Count);
        }
    }
}
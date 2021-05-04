using System.Linq;
using Elsa.Server.Api.Endpoints.WorkflowRegistry;
using Elsa.Server.Api.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Swagger.Examples
{
    public class WorkflowBlueprintModelPagedListExample : IExamplesProvider<PagedList<WorkflowBlueprintModel>>
    {
        public PagedList<WorkflowBlueprintModel> GetExamples()
        {
            var examples = Enumerable.Range(1, 3).Select(_ => new WorkflowBlueprintModelExample().GetExamples()).ToList();
            return new PagedList<WorkflowBlueprintModel>(examples, 0, 10, examples.Count);
        }
    }
}
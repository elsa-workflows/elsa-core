using System.Linq;
using Elsa.Models;
using Elsa.Server.Api.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Swagger.Examples
{
    public class WorkflowDefinitionPagedListExample : IExamplesProvider<PagedList<WorkflowDefinition>>
    {
        public PagedList<WorkflowDefinition> GetExamples()
        {
            var workflowDefinitionExamples = Enumerable.Range(1, 3).Select(_ => new WorkflowDefinitionExample().GetExamples()).ToList();
            return new PagedList<WorkflowDefinition>(workflowDefinitionExamples, 0, 10, workflowDefinitionExamples.Count);
        }
    }
}
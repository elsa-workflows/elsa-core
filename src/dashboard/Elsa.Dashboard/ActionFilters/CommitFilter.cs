using System.Threading.Tasks;
using Elsa.Persistence;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Elsa.Dashboard.ActionFilters
{
    public class CommitFilter : IAsyncActionFilter
    {
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IWorkflowInstanceStore workflowInstanceStore;

        public CommitFilter(IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowInstanceStore workflowInstanceStore)
        {
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.workflowInstanceStore = workflowInstanceStore;
        }
        
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();
            await workflowDefinitionStore.CommitAsync(context.HttpContext.RequestAborted);
        }
    }
}
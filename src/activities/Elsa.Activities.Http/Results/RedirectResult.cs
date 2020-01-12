using System.Threading;
using System.Threading.Tasks;
using Elsa.Results;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Results
{
    public class RedirectResult : IActivityExecutionResult
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public RedirectResult(IHttpContextAccessor httpContextAccessor, PathString location, bool permanent)
        {
            Location = location;
            Permanent = permanent;
            this.httpContextAccessor = httpContextAccessor;
        }
        
        public PathString Location { get; }
        public bool Permanent { get; }
        
        public Task ExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var response = httpContextAccessor.HttpContext.Response;
            response.Redirect(Location, Permanent);
            return Task.CompletedTask;
        }
    }
}
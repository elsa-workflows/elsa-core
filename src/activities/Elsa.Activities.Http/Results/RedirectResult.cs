using Elsa.Results;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Results
{
    public class RedirectResult : ActivityExecutionResult
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
        
        protected override void Execute(ActivityExecutionContext activityExecutionContext)
        {
            var response = httpContextAccessor.HttpContext.Response;
            response.Redirect(Location, Permanent);
        }
    }
}
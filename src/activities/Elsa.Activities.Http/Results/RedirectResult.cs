using System;
using Elsa.ActivityResults;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Results
{
    public class RedirectResult : ActivityExecutionResult
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RedirectResult(IHttpContextAccessor httpContextAccessor, Uri location, bool permanent)
        {
            Location = location;
            Permanent = permanent;
            _httpContextAccessor = httpContextAccessor;
        }
        
        public Uri Location { get; }
        public bool Permanent { get; }
        
        protected override void Execute(ActivityExecutionContext activityExecutionContext)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var response = httpContext.Response;
            
            response.Redirect(Location.ToString(), Permanent);
        }
    }
}
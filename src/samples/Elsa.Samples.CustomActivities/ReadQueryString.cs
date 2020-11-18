using Elsa.ActivityResults;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Samples.CustomActivities
{
    /// <summary>
    /// A basic activity that reads and returns the query string as an output value.
    /// </summary>
    public class ReadQueryString : Activity
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReadQueryString(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var query = _httpContextAccessor.HttpContext!.Request.Query;
            
            return Done(query);
        }
    }
}
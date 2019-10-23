using Elsa.Dashboard.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Elsa.Dashboard.ActionFilters
{
    public class LocalhostFilter : IFilterMetadata, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.IsLocal())
                context.Result = new UnauthorizedResult();
        }
    }
}
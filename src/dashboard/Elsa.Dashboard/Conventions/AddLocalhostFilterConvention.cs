using Elsa.Dashboard.ActionFilters;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Elsa.Dashboard.Conventions
{
    /// <summary>
    /// A sample convention that applies an action filter that only allows requests made from the localhost for the Elsa area. 
    /// </summary>
    public class AddLocalhostFilterConvention : ElsaConvention
    {
        protected override void ApplyConvention(ControllerModel controller)
        {
            controller.Filters.Add(new LocalhostFilter());
        }
    }
}
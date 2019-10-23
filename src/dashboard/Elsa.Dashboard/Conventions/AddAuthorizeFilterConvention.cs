using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace Elsa.Dashboard.Conventions
{
    /// <summary>
    /// A sample convention that applies an authorization filter with a given policy name to the Elsa area. 
    /// </summary>
    public class AddAuthorizeFilterConvention : ElsaConvention
    {
        public AddAuthorizeFilterConvention(string policyName)
        {
            PolicyName = policyName;
        }
        
        public string PolicyName { get; }

        protected override void ApplyConvention(ControllerModel controller)
        {
            controller.Filters.Add(new AuthorizeFilter(PolicyName));
        }
    }
}
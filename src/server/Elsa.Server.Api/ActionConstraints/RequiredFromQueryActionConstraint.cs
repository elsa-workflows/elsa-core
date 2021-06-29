using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Elsa.Server.Api.ActionConstraints
{
    public class RequiredFromQueryActionConstraint : IActionConstraint
    {
        private readonly string _parameter;
        public RequiredFromQueryActionConstraint(string parameter) => _parameter = parameter;
        public int Order => 999;
        public bool Accept(ActionConstraintContext context) => context.RouteContext.HttpContext.Request.Query.ContainsKey(_parameter);
    }
}
using System.Linq;
using Elsa.Server.Api.ActionConstraints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Elsa.Server.Api.Attributes
{
    public class RequiredFromQueryAttribute : FromQueryAttribute, IParameterModelConvention
    {
        public void Apply(ParameterModel parameter)
        {
            if (parameter.Action.Selectors != null && parameter.Action.Selectors.Any())
            {
                parameter.Action.Selectors.Last().ActionConstraints.Add(new RequiredFromQueryActionConstraint(parameter.BindingInfo?.BinderModelName ?? parameter.ParameterName));
            }
        }
    }
}
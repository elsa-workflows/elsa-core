using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Elsa.AspNetCore;

/// <summary>
/// A custom controller model convention that applies a controller & action name based on the values provided via <see cref="ApiEndpointAttribute"/>.
/// </summary>
public class ApiEndpointAttributeConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var controllerNameAttribute = controller.Attributes.OfType<ApiEndpointAttribute>().SingleOrDefault();

        if (controllerNameAttribute != null)
        {
            controller.ControllerName = controllerNameAttribute.ControllerName;
            var actionModel = controller.Actions.FirstOrDefault();

            if (actionModel != null)
                actionModel.ActionName = controllerNameAttribute.ActionName;
        }
    }
}
namespace Elsa.AspNetCore;

/// <summary>
/// Apply this to a controller that models an API endpoint, where the controller contains a single action.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ApiEndpointAttribute : Attribute
{
    public string ControllerName { get; }
    public string ActionName { get; }

    public ApiEndpointAttribute(string controllerName, string actionName)
    {
        ControllerName = controllerName;
        ActionName = actionName;
    }
}
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;

namespace Elsa.Samples.AspNet.CustomUIHandler;

/// <summary>
/// A sample activity that let's you select a car brand.
/// </summary>
public class VehicleActivity : Activity<string>
{
    [Input(
        Description = "The content type to use when sending the request.",
        UIHint = InputUIHints.DropDown,
        UIHandlers = [typeof(VehicleUIHandler), typeof(RefreshUIHandler)]
    )]
    public Input<string> Brand { get; set; } = default!;
}
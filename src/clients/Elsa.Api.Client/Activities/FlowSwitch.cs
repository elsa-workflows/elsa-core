using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Activities;

/// <summary>
/// Represents a flow switch activity.
/// </summary>
public class FlowSwitch : Activity
{
    /// <summary>
    /// Gets or sets the cases.
    /// </summary>
    public ICollection<Case>? Cases
    {
        get => this.TryGetValue<ICollection<Case>>("cases");
        set => this["cases"] = value!;
    }
}
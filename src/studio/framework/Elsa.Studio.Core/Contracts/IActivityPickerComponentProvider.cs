using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Defines a provider for an activity picker component,
/// allowing for the retrieval of a RenderFragment to represent the component.
/// </summary>
public interface IActivityPickerComponentProvider
{
    /// <summary>
    /// Retrieves a RenderFragment that represents the activity picker component.
    /// </summary>
    /// <returns>A RenderFragment instance containing the activity picker component.</returns>
    RenderFragment GetActivityPickerComponent();
}
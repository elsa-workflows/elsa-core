using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.ActivityPickers.Accordion;

/// <summary>
/// Represents a provider for an Accordion-based activity picker component.
/// </summary>
public class AccordionActivityPickerComponentProvider : IActivityPickerComponentProvider
{
    /// <summary>
    /// Resolves the display name for a given category string.
    /// </summary>
    /// <remarks>The default behavior is to extract the first segment of the category string.</remarks>
    public Func<string, string> CategoryDisplayResolver = category => category.Split('/').First().Trim();

    /// <inheritdoc />
    public RenderFragment GetActivityPickerComponent() => builder =>
    {
        builder.OpenComponent<ActivityPicker>(0);
        builder.AddAttribute(1, nameof(ActivityPicker.CategoryDisplayResolver), CategoryDisplayResolver);
        builder.CloseComponent();
    };
}
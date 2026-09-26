using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners;

/// <summary>
/// Renders the toolbox items shared by the diagram designers -- a tooltipped icon button that
/// invokes an action when clicked.
/// </summary>
public static class DiagramDesignerToolbox
{
    /// <summary>
    /// Renders a single toolbox item: a <see cref="MudIconButton"/> wrapped in a <see cref="MudTooltip"/>.
    /// </summary>
    /// <param name="title">The accessible label for the button.</param>
    /// <param name="icon">The icon to display on the button.</param>
    /// <param name="description">The tooltip text shown on hover.</param>
    /// <param name="onClick">The action to invoke when the button is clicked.</param>
    public static RenderFragment DisplayToolboxItem(string title, string icon, string description, Func<Task> onClick)
    {
        return builder =>
        {
            builder.OpenComponent<MudTooltip>(0);
            builder.AddAttribute(1, nameof(MudTooltip.Text), description);
            builder.AddAttribute(2, nameof(MudTooltip.Delay), 500d);
            builder.AddAttribute(3, nameof(MudTooltip.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MudIconButton>(0);
                childBuilder.AddAttribute(1, nameof(MudIconButton.Icon), icon);
                childBuilder.AddAttribute(2, nameof(MudIconButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(onClick.Target!, onClick));
                childBuilder.AddAttribute(3, "aria-label", title);
                childBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }
}

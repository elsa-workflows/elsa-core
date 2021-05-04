using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Shared
{
    partial class ContextMenu
    {
        [Parameter]public RenderFragment ChildContent { get; set; } = default!;
    }
}
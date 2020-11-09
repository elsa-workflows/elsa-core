using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ElsaDashboard.Blazor.Shared.Pages.Designer
{
    partial class Activity
    {
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }
    }
}
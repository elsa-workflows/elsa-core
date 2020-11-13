using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ElsaDashboard.Blazor.Application.Pages.Designer
{
    partial class Activity
    {
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }
    }
}
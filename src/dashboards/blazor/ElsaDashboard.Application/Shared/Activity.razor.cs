using ElsaDashboard.Application.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ElsaDashboard.Application.Shared
{
    partial class Activity
    {
        [Parameter] public ActivityModel Model { get; set; } = new ActivityModel("", "");
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    }
}
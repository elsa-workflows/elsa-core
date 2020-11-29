using Elsa.Client.Models;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Shared
{
    partial class ActivityEditor
    {
        [Parameter] public ActivityInfo ActivityInfo { get; set; } = default!;
    }
}
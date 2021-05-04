using Elsa.Client.Models;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Shared
{
    partial class ActivityEditor
    {
        [Parameter] public ActivityDescriptor ActivityDescriptor { get; set; } = default!;
    }
}
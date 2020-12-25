using ElsaDashboard.Models;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Components
{
    public abstract class ActivityDisplayComponent : ComponentBase
    {
        [Parameter] public ActivityDisplayContext DisplayContext { protected get; set; } = default!;
    }
}
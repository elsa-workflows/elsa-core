using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Components;

/// <summary>
/// Represents the error.
/// </summary>
public partial class Error : ComponentBase
{
    /// <summary>
    /// Gets or sets the exception to display.
    /// </summary>
    [Parameter] public Exception Context { get; set; } = null!;
}

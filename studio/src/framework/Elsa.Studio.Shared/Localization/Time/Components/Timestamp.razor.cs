using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Localization.Time.Components;

/// <summary>
/// Represents the timestamp.
/// </summary>
public partial class Timestamp : ComponentBase
{
    /// <summary>
    /// Gets or sets the timestamp to display.
    /// </summary>
    [Parameter] public DateTimeOffset? Value { get; set; }
    
    /// <summary>
    /// Gets or sets the format to use when displaying the timestamp.
    /// </summary>
    [Parameter] public string Format { get; set; } = "G";
    
    /// <summary>
    /// Gets or sets the string to display when <see cref="Value"/> is <c>null</c>.
    /// </summary>
    [Parameter] public string EmptyString { get; set; } = string.Empty;
    
    [Inject] private ITimeFormatter TimeFormatter { get; set; } = null!;
    
    private string GetDisplayString()
    {
        return Value == null ? EmptyString : TimeFormatter.Format(Value.Value, Format);
    }
}
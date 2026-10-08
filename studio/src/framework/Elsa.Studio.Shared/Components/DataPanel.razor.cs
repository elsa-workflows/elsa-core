using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Elsa.Studio.Components.DataPanelRenderers;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Components;

/// <summary>
/// A component that displays a panel of data items in a tabular format.
/// </summary>
public partial class DataPanel : ComponentBase
{
    /// <summary>
    /// The default maximum length for truncating displayed content in data panel components.
    /// </summary>
    public const int DefaultTruncationLength = 300;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    /// <summary>
    /// The data to display.
    /// </summary>
    [Parameter] public DataPanelModel Data { get; set; } = new();

    /// <summary>
    /// If true, empty values will be hidden.
    /// </summary>
    [Parameter] public bool HideEmptyValues { get; set; }

    /// <summary>
    /// If true, a message will be displayed when there is no data.
    /// </summary>
    [Parameter] public bool ShowNoDataAlert { get; set; }

    /// <summary>
    /// The title of the data panel
    /// </summary>
    [Parameter] public string Title { get; set; } = null!;

    /// <summary>
    /// The message to display when there is no data.
    /// </summary>
    [Parameter] public string? NoDataMessage { get; set; }

    [Inject] private ILocalizer Localizer { get; set; } = null!;
    [Inject] private IClipboard Clipboard { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ITimeFormatter TimeFormatter { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        NoDataMessage ??= Localizer["No data available."];
    }

    private RenderFragment RenderValue(DataPanelItem item) => builder =>
    {
        // Priority 1: ValueTemplate (custom render fragment with context)
        if (item.ValueTemplate != null)
        {
            var context = new DataPanelItemContext
            {
                Label = item.Label,
                Value = item.Value,
                Item = item
            };
            builder.AddContent(0, item.ValueTemplate(context));
            return;
        }

        // Priority 2: ValueComponentType (reusable custom component)
        if (item.ValueComponentType != null)
        {
            builder.OpenComponent(0, item.ValueComponentType);
            builder.AddAttribute(1, "Item", item);
            builder.CloseComponent();
            return;
        }

        // Priority 3: Format-based rendering (new system using dedicated components)
        var displayText = GetFormattedValue(item);
        builder.OpenComponent<DataPanelValueRenderer>(0);
        builder.AddAttribute(1, "Item", item);
        builder.AddAttribute(2, "DisplayText", displayText);
        builder.AddAttribute(3, "TruncationLength", DefaultTruncationLength);
        builder.AddAttribute(4, "OnViewClicked", EventCallback.Factory.Create(this, () => OnViewClicked(item)));
        builder.CloseComponent();
    };

    /// <summary>
    /// Gets the formatted value for a data panel item.
    /// </summary>
    public string GetFormattedValue(DataPanelItem item)
    {
        // Use Text if explicitly provided (backward compatibility)
        if (!string.IsNullOrWhiteSpace(item.Text))
            return item.Text;

        // No value to format
        if (item.Value == null)
            return string.Empty;

        // Apply format based on Format property
        return item.Format switch
        {
            DataPanelItemFormat.Text => item.Value.ToString() ?? string.Empty,
            DataPanelItemFormat.Timestamp => FormatTimestamp(item.Value, item.FormatString),
            DataPanelItemFormat.Number => FormatNumber(item.Value),
            DataPanelItemFormat.Boolean => FormatBoolean(item.Value),
            DataPanelItemFormat.Json => FormatJson(item.Value),
            DataPanelItemFormat.Code => item.Value.ToString() ?? string.Empty,
            DataPanelItemFormat.Markdown => item.Value.ToString() ?? string.Empty,
            DataPanelItemFormat.Auto => FormatAuto(item.Value),
            _ => item.Value.ToString() ?? string.Empty
        };
    }

    private string FormatTimestamp(object value, string? formatString)
    {
        var timestamp = GetTimestampValue(value);
        return timestamp == null
            ? value.ToString() ?? string.Empty
            : TimeFormatter.Format(timestamp, string.IsNullOrWhiteSpace(formatString) ? "G" : formatString);
    }

    internal static DateTimeOffset? GetTimestampValue(object value) => value switch
    {
        // Values without a kind follow the same UTC convention as the timestamp renderer.
        DateTime { Kind: DateTimeKind.Unspecified } dt => new DateTimeOffset(dt, TimeSpan.Zero),
        DateTime dt => new DateTimeOffset(dt),
        DateTimeOffset dto => dto,
        _ => null
    };

    private string FormatNumber(object value)
    {
        return value switch
        {
            int or long or short or byte => $"{value:N0}",
            double or float or decimal => $"{value:N2}",
            _ => value.ToString() ?? string.Empty
        };
    }

    private string FormatBoolean(object value)
    {
        return value is bool b ? (b ? Localizer["Yes"] : Localizer["No"]) : value.ToString() ?? string.Empty;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Data panel values are runtime UI objects that cannot use a fixed source-generated context.")]
    private string FormatJson(object value)
    {
        return value as string ?? JsonSerializer.Serialize(value, JsonSerializerOptions);
    }

    private string FormatAuto(object value)
    {
        // Auto-detect based on type
        return value switch
        {
            DateTime or DateTimeOffset => FormatTimestamp(value, null),
            bool => FormatBoolean(value),
            int or long or short or byte or double or float or decimal => FormatNumber(value),
            _ => value.ToString() ?? string.Empty
        };
    }

    private async Task OnCopyClicked(DataPanelItem item)
    {
        var textToCopy = !string.IsNullOrWhiteSpace(item.Text) ? item.Text : GetFormattedValue(item);
        await Clipboard.CopyText(textToCopy);
        Snackbar.Add(Localizer["{0} copied", item.Label ?? string.Empty], Severity.Success);
    }

    private async Task OnViewClicked(DataPanelItem item)
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            FullWidth = true,
            MaxWidth = MaxWidth.Large
        };

        // Create a copy with the formatted text for viewing
        var itemToView = item with { Text = GetFormattedValue(item) };

        var parameters = new DialogParameters
        {
            { nameof(DataPanelItem), itemToView }
        };
        await DialogService.ShowAsync<ContentVisualizer>(Localizer["View content"], parameters, options);
    }
}

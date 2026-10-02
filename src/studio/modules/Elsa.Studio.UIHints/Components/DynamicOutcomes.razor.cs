using System.Reflection;
using System.Text.Json;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Converters;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudExtensions;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// Implements a component for the "dynamic-outcomes" UI hint.
/// </summary>
public partial class DynamicOutcomes
{
    private List<string> _items = new();
    private MudChipField<string> _chipField = default!;

    /// <summary>
    /// Gets or sets the editor context.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _items = GetCurrentItems();
    }

    private List<string> GetCurrentItems()
    {
        var input = EditorContext.GetObjectValueOrDefault();
        return ParseJson(input);
    }
    
    private List<string> ParseJson(string? json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        options.Converters.Add(new JsonPrimitiveToStringConverter());
        return JsonParser.ParseJson(json, () => new List<string>(), options);
    }

    private async Task OnValuesChanges(List<string> arg)
    { 
        await EditorContext.UpdateValueOrObjectExpressionAsync(_items);
    }

    private void OnKeyDown(KeyboardEventArgs arg)
    {
        // TODO: This is a hack to get the chips to update when the user presses enter.
        // Ideally, we can configure this on MudChipField, but this is not currently supported. 
        if (arg.Key is not ("Enter" or "Tab")) return;
        
        var setChipsMethod = _chipField.GetType().GetMethod("SetChips", BindingFlags.Instance | BindingFlags.NonPublic);
        setChipsMethod!.Invoke(_chipField, null);
    }
}
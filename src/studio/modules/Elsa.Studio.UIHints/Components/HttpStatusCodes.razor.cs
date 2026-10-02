using System.Reflection;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudExtensions;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// Represents the http status codes.
/// </summary>
public partial class HttpStatusCodes
{
    private List<HttpStatusCodeCase> _items = new();
    private MudChipField<string> _chipField = null!;

    /// <summary>
    /// The editor context.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _items = GetCurrentItems();
    }

    private List<HttpStatusCodeCase> GetCurrentItems()
    {
        var expectedStatusCodes = EditorContext.GetValueOrDefault<ICollection<HttpStatusCodeCase>>()?.ToList() ?? new List<HttpStatusCodeCase>();
        return expectedStatusCodes;
    }

    private async Task OnValuesChanges(List<string>? values)
    {
        // Remove any item from _items that is not in values.
        _items.RemoveAll(x => values?.Contains(x.StatusCode.ToString()) != true);

        // Add any item from values that is not in _items.
        _items.AddRange(values?
            .Where(x => _items.All(y => y.StatusCode.ToString() != x))
            .Select(x => new HttpStatusCodeCase { StatusCode = int.Parse(x) }) ?? new List<HttpStatusCodeCase>());
        
        await EditorContext.UpdateValueAsync(_items);
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
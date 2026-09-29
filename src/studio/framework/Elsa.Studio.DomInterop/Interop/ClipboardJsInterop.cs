using Elsa.Studio.DomInterop.Contracts;
using Microsoft.JSInterop;

namespace Elsa.Studio.DomInterop.Interop;

/// <summary>
/// Provides JavaScript interop functionality for clipboard operations.
/// </summary>
public class ClipboardJsInterop(IJSRuntime jsRuntime) : JsInteropBase(jsRuntime), IClipboard
{
    /// <inheritdoc />
    protected override string ModuleName => "clipboard";

    /// <inheritdoc />
    public async Task CopyText(string text, CancellationToken cancellationToken = default)
    {
        await InvokeAsync(async module => await module.InvokeVoidAsync("copyText", cancellationToken, text));
    }
}
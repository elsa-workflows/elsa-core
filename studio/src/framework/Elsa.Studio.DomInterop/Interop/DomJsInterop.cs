using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Microsoft.JSInterop;

namespace Elsa.Studio.DomInterop.Interop;

/// <summary>
/// Provides JavaScript interop functionality for DOM manipulation.
/// </summary>
public class DomJsInterop(IJSRuntime jsRuntime) : JsInteropBase(jsRuntime), IDomAccessor
{
    /// <inheritdoc />
    protected override string ModuleName => "dom";
    
    /// <inheritdoc />
    public async Task<DomRect> GetBoundingClientRectAsync(ElementRef element, CancellationToken cancellationToken = default) => await InvokeAsync(async module => await module.InvokeAsync<DomRect>("getBoundingClientRect", cancellationToken, element.Match()));
    
    /// <inheritdoc />
    public async Task<double> GetVisibleHeightAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => await InvokeAsync(async module => await module.InvokeAsync<double>("getVisibleHeight", cancellationToken, elementRef.Match()));
    
    /// <inheritdoc />
    public async Task ClickElementAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => await InvokeAsync(async module => await module.InvokeVoidAsync("clickElement", cancellationToken, elementRef.Match()));
}
using Microsoft.JSInterop;

namespace Elsa.Studio.DomInterop.Interop;

/// <summary>
/// Provides a base class for JavaScript interop modules with common functionality.
/// </summary>
public abstract class JsInteropBase : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    /// <summary>
    /// Gets the name of the JavaScript module to import.
    /// </summary>
    protected abstract string ModuleName { get; }

    /// <summary>
    /// Initializes a new instance of the JsInteropBase class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime instance.</param>
    protected JsInteropBase(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => ImportModule(jsRuntime));
    }

    /// <summary>
    /// Imports the JavaScript module asynchronously.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime instance.</param>
    /// <returns>A reference to the imported JavaScript module.</returns>
    public virtual Task<IJSObjectReference> ImportModule(IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", $"./_content/Elsa.Studio.DomInterop/{ModuleName}.entry.js").AsTask();
    }

    /// <summary>
    /// Disposes of the JavaScript module reference asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;

            try
            {
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // This exception is thrown when the browser window is closed or the page reloads.
                // We can safely ignore it.
            }
        }
    }

    /// <summary>
    /// Invokes a JavaScript function asynchronously without a return value.
    /// </summary>
    /// <param name="func">The function to invoke on the module.</param>
    protected async Task InvokeAsync(Func<IJSObjectReference, ValueTask> func)
    {
        var module = await _moduleTask.Value;
        await func(module);
    }

    /// <summary>
    /// Invokes a JavaScript function asynchronously with a return value.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="func">The function to invoke on the module.</param>
    /// <returns>The result of the JavaScript function call.</returns>
    protected async Task<T> InvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func)
    {
        var module = await _moduleTask.Value;
        return await func(module);
    }
}
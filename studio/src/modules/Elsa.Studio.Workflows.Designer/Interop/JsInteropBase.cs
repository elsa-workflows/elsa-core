using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

/// <summary>
/// Provides access to the designer JavaScript module.
/// </summary>
public abstract class JsInteropBase : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    /// <summary>
    /// Gets the name of the module to import.
    /// </summary>
    protected abstract string ModuleName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsInteropBase"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime used to import the module.</param>
    protected JsInteropBase(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => ImportModule(jsRuntime));
    }

    public virtual Task<IJSObjectReference> ImportModule(IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", $"./_content/Elsa.Studio.Workflows.Designer/{ModuleName}.entry.js").AsTask();
    }

    /// <summary>
    /// Disposes the imported JavaScript module if it has been created.
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
    /// Invokes the specified callback using the imported module instance.
    /// </summary>
    /// <param name="func">The callback to execute.</param>
    protected async Task InvokeAsync(Func<IJSObjectReference, ValueTask> func)
    {
        var module = await _moduleTask.Value;
        await func(module);
    }

    /// <summary>
    /// Safely invokes the specified callback and ignores JavaScript exceptions.
    /// </summary>
    /// <param name="func">The callback to execute.</param>
    protected async Task TryInvokeAsync(Func<IJSObjectReference, ValueTask> func)
    {
        try
        {
            var module = await _moduleTask.Value;
            await func(module);
        }
        catch (JSException)
        {
            // Ignore.
        }
    }

    /// <summary>
    /// Invokes the specified callback and returns its result.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the callback.</typeparam>
    /// <param name="func">The callback to execute.</param>
    /// <returns>The result produced by <paramref name="func"/>.</returns>
    protected async Task<T> InvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func)
    {
        var module = await _moduleTask.Value;
        return await func(module);
    }

    /// <summary>
    /// Invokes the specified callback and returns its result, ignoring JavaScript exceptions.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the callback.</typeparam>
    /// <param name="func">The callback to execute.</param>
    /// <returns>The result produced by <paramref name="func"/>, or the default value of <typeparamref name="T"/> when an exception occurs.</returns>
    protected async Task<T> TryInvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func)
    {
        try
        {
            var module = await _moduleTask.Value;
            return await func(module);
        }
        catch (JSException)
        {
            // Ignore.
        }
        catch (ObjectDisposedException)
        {
            // Ignore.
        }

        return default!;
    }
}
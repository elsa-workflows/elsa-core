using Elsa.Studio.UserTasks.Contracts;
using Microsoft.JSInterop;

namespace Elsa.Studio.UserTasks.Services;

/// <summary>
/// Visibility-aware polling loop. A hidden document skips its tick entirely, so a background tab stops
/// issuing task queries; the first tick after the tab becomes visible again closes the gap with a refresh.
/// </summary>
public sealed class UserTaskPollingService(IJSRuntime jsRuntime) : IUserTaskPollingService, IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async Task RunAsync(TimeSpan interval, Func<CancellationToken, Task> refreshAsync, CancellationToken cancellationToken = default)
    {
        using var timer = new PeriodicTimer(interval);
        var skippedWhileHidden = false;

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            if (!await IsDocumentVisibleAsync(cancellationToken))
            {
                skippedWhileHidden = true;
                continue;
            }

            // A resume after one or more skipped ticks is a gap, so refresh straight away instead of
            // waiting for the next scheduled tick.
            skippedWhileHidden = false;
            await refreshAsync(cancellationToken);
        }

        if (skippedWhileHidden && !cancellationToken.IsCancellationRequested)
            await refreshAsync(cancellationToken);
    }

    private async Task<bool> IsDocumentVisibleAsync(CancellationToken cancellationToken)
    {
        try
        {
            _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./_content/Elsa.Studio.UserTasks/userTasks.js");
            return await _module.InvokeAsync<bool>("isDocumentVisible", cancellationToken);
        }
        catch (Exception)
        {
            // Prerendering, a disconnected circuit, or a host that cannot load the module: keep polling.
            // A stalled queue is a worse failure than one extra request.
            return true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module == null)
            return;
        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone; nothing to release.
        }
    }
}

using CShells;
using CShells.Configuration;
using CShells.Management;
using Elsa.Workflows.Api.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Api.Services;

internal class ShellReloadOrchestrator(IServiceProvider serviceProvider, ILogger<ShellReloadOrchestrator> logger) : IShellReloadOrchestrator, IDisposable
{
    private static readonly StringComparer ShellIdComparer = StringComparer.OrdinalIgnoreCase;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private bool _disposed;

    public Task<ShellReloadResult> ReloadAllAsync(CancellationToken cancellationToken = default) =>
        ReloadInternalAsync(null, cancellationToken);

    public Task<ShellReloadResult> ReloadAsync(string shellId, CancellationToken cancellationToken = default) =>
        ReloadInternalAsync(shellId, cancellationToken);

    private async Task<ShellReloadResult> ReloadInternalAsync(string? requestedShellId, CancellationToken cancellationToken)
    {
        var requested = string.IsNullOrWhiteSpace(requestedShellId) ? null : requestedShellId;

        if (!await _reloadLock.WaitAsync(0, cancellationToken))
            return CreateBusyResult(requested);

        try
        {
            var shellManager = serviceProvider.GetService<IShellManager>();
            var shellSettingsProvider = serviceProvider.GetService<IShellSettingsProvider>();
            var shellSettingsCache = serviceProvider.GetService<IShellSettingsCache>();

            if (shellManager == null || shellSettingsProvider == null || shellSettingsCache == null)
            {
                logger.LogWarning("Shell reload was requested, but the current host does not provide CShells management services.");
                return CreateFailureResult(requested, "Shell reload is not available in the current host.");
            }

            IReadOnlyDictionary<string, ShellSettings> currentShells;
            IReadOnlyDictionary<string, ShellSettings> latestShells;

            try
            {
                currentShells = shellSettingsCache.GetAll().Select(Clone).ToDictionary(x => x.Id.Name, ShellIdComparer);
                latestShells = (await shellSettingsProvider.GetShellSettingsAsync(cancellationToken)).Select(Clone).ToDictionary(x => x.Id.Name, ShellIdComparer);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to load shell settings from the provider.");
                return CreateFailureResult(requested, exception.Message);
            }

            if (requested != null && !latestShells.ContainsKey(requested))
                return CreateNotFoundResult(requested);

            var itemResults = new List<ShellReloadItemResult>();

            foreach (var currentShell in currentShells.Values
                                                   .Where(x => !latestShells.ContainsKey(x.Id.Name))
                                                   .OrderBy(x => x.Id.Name, ShellIdComparer))
            {
                var result = await RemoveShellAsync(shellManager, currentShell.Id, requested, cancellationToken);
                itemResults.Add(result);
            }

            foreach (var latestShell in latestShells.Values.OrderBy(x => x.Id.Name, ShellIdComparer))
            {
                currentShells.TryGetValue(latestShell.Id.Name, out var previousShell);
                var result = await UpsertShellAsync(shellManager, latestShell, previousShell, requested, cancellationToken);
                itemResults.Add(result);
            }

            return CreateResult(requested, itemResults);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private async Task<ShellReloadItemResult> RemoveShellAsync(IShellManager shellManager, ShellId shellId, string? requestedShellId, CancellationToken cancellationToken)
    {
        try
        {
            await shellManager.RemoveShellAsync(shellId, cancellationToken);

            return new ShellReloadItemResult
            {
                ShellId = shellId.Name,
                Outcome = ShellReloadItemOutcome.Removed,
                Requested = IsRequested(shellId.Name, requestedShellId)
            };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to remove shell '{ShellId}' during reload.", shellId.Name);

            return new ShellReloadItemResult
            {
                ShellId = shellId.Name,
                Outcome = ShellReloadItemOutcome.InvalidConfiguration,
                Requested = IsRequested(shellId.Name, requestedShellId),
                Message = exception.Message
            };
        }
    }

    private async Task<ShellReloadItemResult> UpsertShellAsync(IShellManager shellManager, ShellSettings latestShell, ShellSettings? previousShell, string? requestedShellId, CancellationToken cancellationToken)
    {
        try
        {
            if (previousShell == null)
                await shellManager.AddShellAsync(Clone(latestShell), cancellationToken);
            else
                await shellManager.UpdateShellAsync(Clone(latestShell), cancellationToken);

            return new ShellReloadItemResult
            {
                ShellId = latestShell.Id.Name,
                Outcome = ShellReloadItemOutcome.Reloaded,
                Requested = IsRequested(latestShell.Id.Name, requestedShellId)
            };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to reload shell '{ShellId}'.", latestShell.Id.Name);
            var message = exception.Message;

            try
            {
                await shellManager.RemoveShellAsync(latestShell.Id, cancellationToken);

                if (previousShell != null)
                    await shellManager.AddShellAsync(Clone(previousShell), cancellationToken);
            }
            catch (Exception restoreException)
            {
                logger.LogWarning(restoreException, "Failed to restore the previous configuration for shell '{ShellId}'.", latestShell.Id.Name);
                message = $"{message} Previous configuration could not be restored: {restoreException.Message}";
            }

            return new ShellReloadItemResult
            {
                ShellId = latestShell.Id.Name,
                Outcome = ShellReloadItemOutcome.InvalidConfiguration,
                Requested = IsRequested(latestShell.Id.Name, requestedShellId),
                Message = message
            };
        }
    }

    private static ShellReloadResult CreateBusyResult(string? requestedShellId)
    {
        var shells = requestedShellId == null ? Array.Empty<ShellReloadItemResult>() :
        [
            new ShellReloadItemResult
            {
                ShellId = requestedShellId,
                Outcome = ShellReloadItemOutcome.Skipped,
                Requested = true,
                Message = "A shell reload is already in progress."
            }
        ];

        return new ShellReloadResult
        {
            Status = ShellReloadStatus.Busy,
            RequestedShellId = requestedShellId,
            ReloadedAt = DateTimeOffset.UtcNow,
            Shells = shells
        };
    }

    private static ShellReloadResult CreateFailureResult(string? requestedShellId, string message)
    {
        var shells = requestedShellId == null ? Array.Empty<ShellReloadItemResult>() :
        [
            new ShellReloadItemResult
            {
                ShellId = requestedShellId,
                Outcome = ShellReloadItemOutcome.Skipped,
                Requested = true,
                Message = message
            }
        ];

        return new ShellReloadResult
        {
            Status = ShellReloadStatus.Failed,
            RequestedShellId = requestedShellId,
            ReloadedAt = DateTimeOffset.UtcNow,
            Shells = shells
        };
    }

    private static ShellReloadResult CreateNotFoundResult(string requestedShellId) =>
        new()
        {
            Status = ShellReloadStatus.NotFound,
            RequestedShellId = requestedShellId,
            ReloadedAt = DateTimeOffset.UtcNow,
            Shells =
            [
                new ShellReloadItemResult
                {
                    ShellId = requestedShellId,
                    Outcome = ShellReloadItemOutcome.Unknown,
                    Requested = true,
                    Message = "The requested shell was not found in the current configuration source."
                }
            ]
        };

    private static ShellReloadResult CreateResult(string? requestedShellId, IReadOnlyCollection<ShellReloadItemResult> shellResults)
    {
        var requestedResult = requestedShellId == null
            ? null
            : shellResults.FirstOrDefault(x => IsRequested(x.ShellId, requestedShellId));
        var hasFailures = shellResults.Any(x => x.Outcome is ShellReloadItemOutcome.InvalidConfiguration or ShellReloadItemOutcome.Unknown or ShellReloadItemOutcome.Skipped);
        var requestedFailed = requestedResult != null && requestedResult.Outcome != ShellReloadItemOutcome.Reloaded;

        return new ShellReloadResult
        {
            Status = requestedFailed
                ? ShellReloadStatus.RequestedShellFailed
                : hasFailures ? ShellReloadStatus.Partial : ShellReloadStatus.Completed,
            RequestedShellId = requestedShellId,
            ReloadedAt = DateTimeOffset.UtcNow,
            Shells = shellResults.OrderBy(x => x.ShellId, ShellIdComparer).ToArray()
        };
    }

    private static bool IsRequested(string shellId, string? requestedShellId) =>
        requestedShellId != null && ShellIdComparer.Equals(shellId, requestedShellId);

    private static ShellSettings Clone(ShellSettings source)
    {
        var clone = new ShellSettings(source.Id, source.EnabledFeatures)
        {
            ConfigurationData = new Dictionary<string, object>(source.ConfigurationData, StringComparer.OrdinalIgnoreCase)
        };

        foreach (var configurator in source.FeatureConfigurators)
            clone.FeatureConfigurators[configurator.Key] = configurator.Value;

        return clone;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _reloadLock.Dispose();
        _disposed = true;
    }
}

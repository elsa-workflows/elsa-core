using JetBrains.Annotations;
using Nuplane.Abstractions;

namespace Elsa.ModularServer.Web;

[UsedImplicitly]
public class MyPackageObserver(ILogger<MyPackageObserver> logger) : INuplaneObserver
{
    public Task OnPackagesChangingAsync(PackageChangeSet changeSet, CancellationToken ct)
    {
        logger.LogInformation("Packages are changing: {ChangeSet}", changeSet);
        return Task.CompletedTask;
    }

    public Task OnPackagesChangedAsync(PackageChangeSet changeSet, CancellationToken ct)
    {
        logger.LogInformation("Packages have changed: {ChangeSet}", changeSet);
        return Task.CompletedTask;
    }

    public Task OnPackageFailedAsync(string packageId, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Package '{PackageId}' failed to load", packageId);
        return Task.CompletedTask;
    }
}
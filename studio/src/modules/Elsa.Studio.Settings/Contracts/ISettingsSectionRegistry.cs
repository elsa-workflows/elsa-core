using Elsa.Studio.Settings.Models;

namespace Elsa.Studio.Settings.Contracts;

/// <summary>Returns the permission-filtered, deterministic settings navigation model.</summary>
public interface ISettingsSectionRegistry
{
    ValueTask<IReadOnlyList<SettingsSectionDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}

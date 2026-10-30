using Elsa.Studio.Settings.Models;

namespace Elsa.Studio.Settings.Contracts;

/// <summary>Contributes caller-visible settings destinations.</summary>
public interface ISettingsSectionProvider
{
    ValueTask<IEnumerable<SettingsSectionDescriptor>> GetSectionsAsync(CancellationToken cancellationToken = default);
}

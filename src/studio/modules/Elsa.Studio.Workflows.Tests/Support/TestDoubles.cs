using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.Extensions.Localization;

namespace Elsa.Studio.Workflows.Tests.Support;

/// <summary>
/// An <see cref="ILocalizer"/> that passes every key straight through, formatting arguments where given, so tests
/// can assert on the text a component renders without wiring up real localization resources.
/// </summary>
internal sealed class TestLocalizer : ILocalizer
{
    public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
    public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
}

/// <summary>
/// An <see cref="IActivityRegistry"/> backed by a fixed, in-memory set of descriptors, for tests that need the
/// registry's lookups without a real backend behind it.
/// </summary>
internal sealed class TestActivityRegistry(IEnumerable<ActivityDescriptor> activities) : IActivityRegistry
{
    public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public IEnumerable<ActivityDescriptor> List() => activities;
    public ActivityDescriptor? Find(string activityType, int? version = null) => activities.FirstOrDefault(x => x.TypeName == activityType);
    public IEnumerable<ActivityDescriptor> FindAll(string activityType) => activities.Where(x => x.TypeName == activityType);

    public void MarkStale()
    {
    }
}

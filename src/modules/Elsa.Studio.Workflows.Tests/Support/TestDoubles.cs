using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Shared.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Elsa.Studio.Workflows.Tests.Support;

/// <summary>
/// An <see cref="ITimeFormatter"/> that formats timestamps as they are, without time zone conversion.
/// </summary>
internal sealed class TestTimeFormatter : ITimeFormatter
{
    public string Format(DateTimeOffset? value, string format = "G", string emptyString = "") => value?.ToString(format) ?? emptyString;
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

/// <summary>
/// A <see cref="DiagramDesignerWrapper"/> that renders only its toolbar slot, which carries the permission-gated
/// actions of the editor and the instance viewer, without the designer canvas behind it.
/// </summary>
internal sealed class TestDiagramDesignerWrapper : DiagramDesignerWrapper
{
    protected override Task OnInitializedAsync() => Task.CompletedTask;

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, CustomToolbarItems);
}

/// <summary>
/// An <see cref="IExpressionService"/> that offers a fixed pair of expression descriptors.
/// </summary>
internal sealed class StubExpressionService : IExpressionService
{
    private static readonly ExpressionDescriptor[] Descriptors = [new("Literal", "Literal"), new("JavaScript", "JavaScript")];

    public Task<IEnumerable<ExpressionDescriptor>> ListDescriptorsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ExpressionDescriptor>>(Descriptors);
    public Task<ExpressionDescriptor?> GetByTypeAsync(string type, CancellationToken cancellationToken = default) => Task.FromResult(Descriptors.FirstOrDefault(x => x.Type == type));
}

/// <summary>
/// An <see cref="IRemoteFeatureProvider"/> that reports every feature as enabled.
/// </summary>
internal sealed class EnabledRemoteFeatureProvider : IRemoteFeatureProvider
{
    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

/// <summary>
/// An <see cref="IWorkflowInstanceObserverFactory"/> for tests whose designer never gets as far as observing an instance.
/// </summary>
internal sealed class UnusedObserverFactory : IWorkflowInstanceObserverFactory
{
    public Task<IWorkflowInstanceObserver> CreateAsync(string workflowInstanceId) => throw new NotSupportedException();
    public Task<IWorkflowInstanceObserver> CreateAsync(WorkflowInstanceObserverContext context) => throw new NotSupportedException();
}

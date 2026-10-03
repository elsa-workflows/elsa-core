using System.Reflection;
using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.VersionHistory;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// The instance designer, the version history and the activity properties panel only offer the actions the user's
/// permissions allow. The workflow editor's own toolbar is covered by <see cref="WorkflowEditorLifecycleTests"/>.
/// </summary>
public sealed class WorkflowActionPermissionTests : BunitContext, IAsyncLifetime
{
    private const string ViewOnly = "workflows/definitions:view";
    private readonly IRenderedComponent<MudPopoverProvider> _popovers;

    public WorkflowActionPermissionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddCoreInternal();
        Services.AddRemoteBackend();
        Services.AddWorkflowsModule();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IActivityRegistry>(new TestActivityRegistry([]));
        Services.AddSingleton<IWorkflowDefinitionService, VersionsWorkflowDefinitionService>();
        Services.AddSingleton<IRemoteFeatureProvider, EnabledRemoteFeatureProvider>();
        Services.AddSingleton<IExpressionService, StubExpressionService>();
        Services.AddSingleton<IWorkflowInstanceObserverFactory, UnusedObserverFactory>();
        ComponentFactories.Add<DiagramDesignerWrapper, TestDiagramDesignerWrapper>();
        _popovers = Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Theory]
    [InlineData(new[] { ViewOnly, "workflows/instances:view" }, false)]
    [InlineData(new[] { ViewOnly, "workflows/instances:view", "alterations:execute" }, true)]
    public void InstanceDesigner_OffersAlterOnlyWithTheAlterPermission(string[] grants, bool canAlter)
    {
        var instance = new WorkflowInstance { Id = "instance-1", DefinitionId = "definition-1", Status = WorkflowStatus.Running };
        var cut = Render<RunningInstanceDesigner>(parameters => parameters
            .Add(x => x.WorkflowDefinition, new WorkflowDefinition { Id = "version-1", DefinitionId = "definition-1" })
            .Add(x => x.WorkflowInstance, instance)
            .AddCascadingValue(StubPermissionService.Grants(grants)));

        var actions = cut.FindComponents<MudIconButton>().Select(x => x.Instance.Icon).ToList();
        Assert.Contains(Icons.Material.Outlined.EditNote, actions);
        Assert.Equal(canAlter, actions.Contains(Icons.Material.Outlined.Tune));
    }

    [Theory]
    [InlineData(new[] { ViewOnly }, false, false)]
    [InlineData(new[] { ViewOnly, "workflows/definitions/versions:revert" }, true, false)]
    [InlineData(new[] { ViewOnly, "workflows/definitions:delete" }, false, true)]
    public void VersionHistory_OffersRollbackAndDeleteOnlyWithTheirPermissions(string[] grants, bool canRollback, bool canDelete)
    {
        var cut = Render<VersionHistoryTab>(parameters => parameters
            .Add(x => x.DefinitionId, "definition-1")
            .AddCascadingValue(EditableWorkspace())
            .AddCascadingValue(StubPermissionService.Grants(grants)));

        var bulkActions = cut.FindAll(".mud-menu").FirstOrDefault(x => x.TextContent.Contains("Bulk actions"));
        cut.WaitForElements("tbody .mud-menu button");
        cut.FindAll("tbody .mud-menu button").Last().Click(); // The older version, the only row that can be rolled back to.

        var items = _popovers.FindAll(".mud-menu-item").Select(x => x.TextContent.Trim()).ToList();
        Assert.Contains("View", items);
        Assert.Equal(canRollback, items.Contains("Rollback to this version"));
        Assert.Equal(canDelete, items.Contains("Delete"));
        Assert.Equal(canDelete, bulkActions is not null);

        // The definition being viewed is editable, so every action the user is granted must also be usable.
        var menuItems = _popovers.FindAll(".mud-menu-item");
        Assert.All(menuItems.Where(x => x.TextContent.Trim() is "Rollback to this version" or "Delete"), x => Assert.False(x.HasAttribute("disabled") || x.ClassList.Contains("mud-disabled")));
        if (canDelete)
            Assert.DoesNotContain("mud-disabled", bulkActions!.ClassList.Concat(bulkActions.QuerySelectorAll("*").SelectMany(x => x.ClassList)));
    }

    /// <summary>A workspace viewing a definition that carries the <c>publish</c> link, which is what makes it editable.</summary>
    private static WorkflowDefinitionWorkspace EditableWorkspace()
    {
        var workspace = new WorkflowDefinitionWorkspace();
        var editable = new WorkflowDefinition { Id = "definition-1:2", DefinitionId = "definition-1", Links = [new("/publish", "publish", "POST")] };
        typeof(WorkflowDefinitionWorkspace).GetField("_selectedWorkflowDefinition", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(workspace, editable);
        return workspace;
    }

    [Theory]
    [InlineData(new[] { ViewOnly }, false)]
    [InlineData(new[] { ViewOnly, "workflows/tests:execute" }, true)]
    public void ActivityProperties_OffersTheTestTabOnlyWithTheTestPermission(string[] grants, bool canTest)
    {
        var cut = Render<ActivityPropertiesPanel>(parameters => parameters.AddCascadingValue(StubPermissionService.Grants(grants)));

        cut.WaitForElements(".mud-tab");
        var tabs = cut.FindComponents<MudTabPanel>().Select(x => x.Instance.Text).ToList();
        Assert.Contains("Common", tabs);
        Assert.Equal(canTest, tabs.Contains("Test"));
    }

    /// <summary>A <see cref="WorkflowInstanceDesigner"/> that keeps its markup but skips the first-render designer setup.</summary>
    private sealed class RunningInstanceDesigner : WorkflowInstanceDesigner
    {
        protected override Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;
    }

    private sealed class VersionsWorkflowDefinitionService : ThrowingWorkflowDefinitionServiceBase
    {
        public override Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedListResponse<WorkflowDefinitionSummary>
            {
                Items = [new() { Id = "definition-1:2", DefinitionId = "definition-1", Version = 2, IsLatest = true }, new() { Id = "definition-1:1", DefinitionId = "definition-1", Version = 1 }],
                TotalCount = 2
            });
    }
}

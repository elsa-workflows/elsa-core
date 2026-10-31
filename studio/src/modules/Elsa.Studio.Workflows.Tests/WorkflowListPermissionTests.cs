using System.Reflection;
using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionList;
using Elsa.Studio.Workflows.Components.WorkflowInstanceList;
using Elsa.Studio.Workflows.Components.WorkflowInstanceList.Components;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Tests.Support;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>The workflow definition and instance lists only offer the actions the user's permissions allow.</summary>
public sealed class WorkflowListPermissionTests : BunitContext, IAsyncLifetime
{
    private const string DefinitionName = "Order process";
    private const string InstanceId = "instance-1";
    private readonly IRenderedComponent<MudPopoverProvider> _popovers;

    public WorkflowListPermissionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddMudExtensions();
        Services.AddLogging();
        Services.AddCoreInternal();
        Services.AddRemoteBackend();
        Services.AddWorkflowsModule();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<ITimeFormatter, TestTimeFormatter>();
        Services.AddSingleton(DispatchProxy.Create<IWorkflowDefinitionService, WorkflowDefinitionServiceProxy>());
        Services.AddSingleton(DispatchProxy.Create<IWorkflowInstanceService, WorkflowInstanceServiceProxy>());
        Services.AddSingleton(DispatchProxy.Create<IRemoteFeatureProvider, EnabledRemoteFeatureProviderProxy>());
        _popovers = Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void DefinitionList_ViewOnly_OffersViewingAndExportingOnly()
    {
        var cut = RenderList<WorkflowDefinitionList>(DefinitionName, "workflows/definitions:view");

        Assert.DoesNotContain("Create workflow", cut.Markup);
        var rowActions = OpenRowActions(cut);
        Assert.Contains("View", rowActions);
        Assert.Contains("Export", rowActions);
        Assert.All(new[] { "Edit", "Run", "Duplicate", "Cancel", "Delete", "Publish", "Unpublish" }, action => Assert.DoesNotContain(action, rowActions));
    }

    [Fact]
    public void DefinitionList_FullAccess_OffersEveryAction()
    {
        var cut = RenderList<WorkflowDefinitionList>(DefinitionName, "workflows/*:*");

        Assert.Contains("Create workflow", cut.Markup);
        var rowActions = OpenRowActions(cut);
        Assert.All(new[] { "Edit", "Run", "Duplicate", "Cancel", "Delete", "Publish", "Unpublish", "Export" }, action => Assert.Contains(action, rowActions));
    }

    [Fact]
    public void InstanceList_ViewOnly_OffersViewingOnly()
    {
        var cut = RenderList<WorkflowInstanceList>(InstanceId, "workflows/*:view");

        Assert.DoesNotContain("Bulk actions", cut.Markup);
        var rowActions = OpenRowActions(cut);
        Assert.Contains("View", rowActions);
        Assert.All(new[] { "Alter", "Cancel", "Delete" }, action => Assert.DoesNotContain(action, rowActions));
    }

    [Fact]
    public void InstanceList_FullAccess_OffersEveryAction()
    {
        var cut = RenderList<WorkflowInstanceList>(InstanceId, "workflows/*:*", "alterations:execute");

        Assert.Contains("Bulk actions", cut.Markup);
        var rowActions = OpenRowActions(cut);
        Assert.All(new[] { "View", "Alter", "Cancel", "Delete" }, action => Assert.Contains(action, rowActions));
    }

    [Fact]
    public void InstanceList_AlterationsWithoutCancel_OffersCancellingEveryMatchWithoutASelection()
    {
        var dialogs = Render<MudDialogProvider>();
        var cut = RenderList<WorkflowInstanceList>(InstanceId, "workflows/*:view", "alterations:execute");

        var bulkActions = _popovers.OpenMenu(cut.FindAll("button").Single(x => x.TextContent.Contains("Bulk actions")));
        Assert.DoesNotContain("Delete", _popovers.Markup);
        bulkActions.Single(x => x.TextContent.Trim() == "Cancel").Click();

        dialogs.WaitForAssertion(() => Assert.Contains("Cancel all matching workflow instances?", dialogs.Markup));
    }

    [Fact]
    public async Task BulkCancelDialog_WithoutCancelPermission_OnlyCancelsEveryMatch()
    {
        var dialogs = Render<MudDialogProvider>();
        var parameters = new DialogParameters<BulkCancelDialog>
        {
            { x => x.CanCancelSelected, false },
            { x => x.CanApplyToAllMatches, true }
        };
        var reference = await dialogs.InvokeAsync(() => Services.GetRequiredService<IDialogService>().ShowAsync<BulkCancelDialog>("Cancel", parameters));

        Assert.Empty(dialogs.FindAll("input[type=checkbox]"));
        dialogs.FindAll("button").Single(x => x.TextContent.Trim() == "Yes").Click();

        Assert.True((bool)(await reference.Result)!.Data!);
    }

    // The shell's page guard cascades the user's permissions to the page.
    private IRenderedComponent<TList> RenderList<TList>(string expectedRowText, params string[] grants) where TList : IComponent
    {
        var cut = Render<TList>(parameters => parameters.AddCascadingValue(StubPermissionService.Grants(grants)));
        cut.WaitForAssertion(() => Assert.Contains(expectedRowText, cut.Markup));
        return cut;
    }

    private string OpenRowActions<TList>(IRenderedComponent<TList> cut) where TList : IComponent
    {
        _popovers.OpenMenu(cut.Find("tbody .mud-menu button"));
        return _popovers.Markup;
    }

    private class WorkflowDefinitionServiceProxy : DispatchProxy
    {
        private static readonly Link[] PublishLinks = [new("/publish", "publish", "POST"), new("/bulk-publish", "bulk-publish", "POST")];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod!.Name == nameof(IWorkflowDefinitionService.ListAsync)
                ? Task.FromResult(new PagedListResponse<WorkflowDefinitionSummary>
                {
                    Items = [new() { Id = "order-process:1", DefinitionId = "order-process", Name = DefinitionName, Version = 1, IsLatest = true, Links = PublishLinks }],
                    TotalCount = 1,
                    Links = PublishLinks
                })
                : throw new InvalidOperationException($"Unexpected call to {targetMethod.DeclaringType!.Name}.{targetMethod.Name}.");
    }

    private class WorkflowInstanceServiceProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod!.Name == nameof(IWorkflowInstanceService.ListAsync)
                ? Task.FromResult(new PagedListResponse<WorkflowInstanceSummary>
                {
                    Items = [new() { Id = InstanceId, DefinitionId = "order-process", DefinitionVersionId = "order-process:1", Version = 1, Status = WorkflowStatus.Running, SubStatus = WorkflowSubStatus.Suspended }],
                    TotalCount = 1
                })
                : throw new InvalidOperationException($"Unexpected call to {targetMethod.DeclaringType!.Name}.{targetMethod.Name}.");
    }

    private class EnabledRemoteFeatureProviderProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod!.Name == nameof(IRemoteFeatureProvider.IsEnabledAsync)
                ? Task.FromResult(true)
                : throw new InvalidOperationException($"Unexpected call to {targetMethod.DeclaringType!.Name}.{targetMethod.Name}.");
    }
}

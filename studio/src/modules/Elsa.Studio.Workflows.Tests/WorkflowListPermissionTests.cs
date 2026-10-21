using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text;
using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
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
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;
using Refit;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>The workflow definition and instance lists only offer the actions the user's permissions allow.</summary>
public sealed class WorkflowListPermissionTests : BunitContext, IAsyncLifetime
{
    private const string DefinitionName = "Order process";
    private const string InstanceId = "instance-1";
    private IRenderedComponent<MudPopoverProvider>? _popoverProvider;
    private IRenderedComponent<MudPopoverProvider> Popovers => _popoverProvider ??= Render<MudPopoverProvider>();

    public WorkflowListPermissionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddMudExtensions();
        Services.AddLogging();
        Services.AddCoreInternal();
        Services.AddRemoteBackend();
        Services.AddWorkflowsModule();
        // A provider without authenticated request-context binding must not prevent ordinary list publication.
        Services.AddSingleton<IBackendApiClientProvider>(new UnboundBackendApiClientProvider());
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<ITimeFormatter, TestTimeFormatter>();
        Services.AddSingleton(DispatchProxy.Create<IWorkflowDefinitionService, WorkflowDefinitionServiceProxy>());
        Services.AddSingleton(DispatchProxy.Create<IWorkflowInstanceService, WorkflowInstanceServiceProxy>());
        Services.AddSingleton(DispatchProxy.Create<IRemoteFeatureProvider, EnabledRemoteFeatureProviderProxy>());
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
    public async Task DefinitionList_OrdinaryPublishWithLegacyProviderDoesNotReadBackendContext()
    {
        var definitionService = (WorkflowDefinitionServiceProxy)Services.GetRequiredService<IWorkflowDefinitionService>();
        var cut = RenderList<WorkflowDefinitionList>(DefinitionName, "workflows/*:*");

        await ClickRowPublishAsync(cut);

        Assert.Equal(1, definitionService.PublishCallCount);
        Assert.Empty(cut.FindAll("[aria-label='Native compensation publication refusal']"));
    }

    [Fact]
    public async Task DefinitionList_StoredNativeRefusalWithoutRequestBindingShowsOnlySafeSummary()
    {
        var definitionService = (WorkflowDefinitionServiceProxy)Services.GetRequiredService<IWorkflowDefinitionService>();
        definitionService.PublishException = await CreateApiExceptionAsync(NativeCompensationRefusalResponse);
        var cut = RenderList<WorkflowDefinitionList>(DefinitionName, "workflows/*:*");

        await ClickRowPublishAsync(cut);

        var summary = cut.WaitForElement("[aria-label='Native compensation publication refusal'][role='alert'][tabindex='-1']");
        Assert.Contains("Native compensation drafts cannot be published.", summary.TextContent);
        Assert.DoesNotContain("scope-17", summary.TextContent);
        Assert.DoesNotContain("step-9", summary.TextContent);
        Assert.DoesNotContain("order-process", summary.TextContent);
        Assert.DoesNotContain("server-private-message", summary.TextContent);
        Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
        Assert.Equal(1, definitionService.PublishCallCount);
    }

    [Fact]
    public async Task DefinitionList_ClearsQualifiedNativeRefusalWhenPermissionRefreshSignalIsRaised()
    {
        Services.AddSingleton<IBackendApiClientProvider>(new BoundBackendApiClientProvider());
        var signal = Services.GetRequiredService<IPermissionRefreshSignal>();
        var definitionService = (WorkflowDefinitionServiceProxy)Services.GetRequiredService<IWorkflowDefinitionService>();
        definitionService.PublishException = await CreateApiExceptionAsync(NativeCompensationRefusalResponse);
        var cut = RenderList<WorkflowDefinitionList>(DefinitionName, "workflows/*:*");

        await ClickRowPublishAsync(cut);
        var summary = cut.WaitForElement("[aria-label='Native compensation publication refusal'][role='alert'][tabindex='-1']");
        Assert.Contains("scope-17", summary.TextContent);
        Assert.Contains("order-process", summary.TextContent);

        signal.Raise();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[aria-label='Native compensation publication refusal']")));
    }

    [Fact]
    public async Task DefinitionList_StoredUnknownRefusalCodeKeepsExistingExceptionBehavior()
    {
        var definitionService = (WorkflowDefinitionServiceProxy)Services.GetRequiredService<IWorkflowDefinitionService>();
        definitionService.PublishException = await CreateApiExceptionAsync(OtherPublishFailureResponse);
        var cut = RenderList<WorkflowDefinitionList>(DefinitionName, "workflows/*:*");
        var publishHandler = typeof(WorkflowDefinitionList).GetMethod("OnPublishClicked", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var expectedException = definitionService.PublishException!;
        var thrownException = await Assert.ThrowsAnyAsync<ApiException>(() => cut.InvokeAsync(() =>
            (Task)publishHandler.Invoke(cut.Instance, ["order-process"])!));

        Assert.Same(expectedException, thrownException);
        Assert.Equal(1, definitionService.PublishCallCount);
        Assert.Empty(cut.FindAll("[aria-label='Native compensation publication refusal']"));
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

        var bulkActions = Popovers.OpenMenu(cut.FindAll("button").Single(x => x.TextContent.Contains("Bulk actions")));
        Assert.DoesNotContain("Delete", Popovers.Markup);
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
        // Keep setup configurable until each test selects its backend, then create the shared popover host.
        _ = Popovers;
        var cut = Render<TList>(parameters => parameters.AddCascadingValue(StubPermissionService.Grants(grants)));
        cut.WaitForAssertion(() => Assert.Contains(expectedRowText, cut.Markup));
        return cut;
    }

    private string OpenRowActions<TList>(IRenderedComponent<TList> cut) where TList : IComponent
    {
        Popovers.OpenMenu(cut.Find("tbody .mud-menu button"));
        return Popovers.Markup;
    }

    private async Task ClickRowPublishAsync<TList>(IRenderedComponent<TList> cut) where TList : IComponent
    {
        var publishItem = Popovers.OpenMenu(cut.Find("tbody .mud-menu button"))
            .Single(x => x.TextContent.Trim() == "Publish");
        await cut.InvokeAsync(() => publishItem.ClickAsync(new MouseEventArgs()));
    }

    private static async Task<ApiException> CreateApiExceptionAsync(string content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/workflow-definitions/order-process/publish");
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            RequestMessage = request,
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
        return await ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    private const string NativeCompensationRefusalResponse = """
    {
      "errors": { "generalErrors": ["server-private-message"] },
      "code": "NATIVE_COMPENSATION_DRAFT_ONLY",
      "data": { "diagnostics": [{
        "scopeActivityId": "scope-17",
        "stepId": "step-9",
        "role": "forward",
        "relatedActivityIds": ["forward-activity-3"],
        "mappingIds": ["capture-2"],
        "mappingId": "capture-2"
      }] }
    }
    """;

    private const string OtherPublishFailureResponse = """
    {
      "errors": { "generalErrors": ["ordinary publish failure"] },
      "code": "OTHER_FEATURE_REFUSAL",
      "data": { "diagnostics": [{ "scopeActivityId": "scope-17", "role": "forward" }] }
    }
    """;

    private class WorkflowDefinitionServiceProxy : DispatchProxy
    {
        private static readonly Link[] PublishLinks = [new("/publish", "publish", "POST"), new("/bulk-publish", "bulk-publish", "POST")];

        public ApiException? PublishException { get; set; }
        public int PublishCallCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name == nameof(IWorkflowDefinitionService.ListAsync))
                return Task.FromResult(new PagedListResponse<WorkflowDefinitionSummary>
                {
                    Items = [new() { Id = "order-process:1", DefinitionId = "order-process", Name = DefinitionName, Version = 1, IsLatest = true, Links = PublishLinks }],
                    TotalCount = 1,
                    Links = PublishLinks
                });

            if (targetMethod.Name == nameof(IWorkflowDefinitionService.PublishAsync))
            {
                PublishCallCount++;
                if (PublishException != null)
                    throw PublishException;
                return Task.FromResult(new SaveWorkflowDefinitionResponse(new WorkflowDefinition(), false, 0));
            }

            throw new InvalidOperationException($"Unexpected call to {targetMethod.DeclaringType!.Name}.{targetMethod.Name}.");
        }
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

    private sealed class UnboundBackendApiClientProvider : IBackendApiClientProvider
    {
        public Uri Url => new("http://localhost");
        public string ContextKey => throw new InvalidOperationException("Ordinary list publication must not read a legacy backend context.");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
    }

    private sealed class BoundBackendApiClientProvider : IBackendApiClientProvider, IBackendRequestContextBinding
    {
        public Uri Url => new("http://localhost");
        public string ContextKey => "bound-test-context";
        public bool SupportsAuthenticatedRequestContextBinding => true;
        public bool HasActiveRequestContextBinding => false;

        public void RegisterAuthenticatedRequestTransport(IBackendRequestContextEpochSource? epochSource)
        {
        }

        public IDisposable BindRequestContext(string expectedContextKey)
        {
            if (!string.Equals(expectedContextKey, ContextKey, StringComparison.Ordinal))
                throw new BackendRequestContextChangedException();

            return new NoopDisposable();
        }

        public void ValidateBoundRequestContext()
        {
        }

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private class EnabledRemoteFeatureProviderProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod!.Name == nameof(IRemoteFeatureProvider.IsEnabledAsync)
                ? Task.FromResult(true)
                : throw new InvalidOperationException($"Unexpected call to {targetMethod.DeclaringType!.Name}.{targetMethod.Name}.");
    }
}

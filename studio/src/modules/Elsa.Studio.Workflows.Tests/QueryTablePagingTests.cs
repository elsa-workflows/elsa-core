using System.Reflection;
using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionList;
using Elsa.Studio.Workflows.Components.WorkflowInstanceList;
using Elsa.Studio.Workflows.Components.WorkflowInstanceList.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Tests.Support;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Studio list tables must keep <c>?page=N</c> across row selection and when the list is opened from that URL
/// (browser back after viewing an instance). See #7447.
/// </summary>
public sealed class QueryTablePagingTests : BunitContext, IAsyncLifetime
{
    private const string PageTwoInstanceId = "instance-p1-0";
    private const string PageTwoDefinitionName = "Definition page 2";
    private readonly WorkflowInstanceServiceProxy _instances = new();
    private readonly WorkflowDefinitionServiceProxy _definitions = new();
    private readonly IRenderedComponent<MudPopoverProvider> _popovers;

    public QueryTablePagingTests()
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
        Services.AddSingleton(CreateInstanceService());
        Services.AddSingleton(CreateDefinitionService());
        Services.AddSingleton<IRemoteFeatureProvider>(new EnabledRemoteFeatureProvider());
        Services.Configure<WorkflowInstanceListPollingOptions>(opts =>
        {
            opts.IsEnabledByDefault = false;
            opts.IntervalSeconds = 3600;
        });
        _popovers = Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void Reads_page_and_page_size_from_the_url_before_first_render()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/workflows/instances?page=3&pageSize=25");

        var cut = Render<TestQueryTable>();

        Assert.Equal(2, cut.Instance.PageOnFirstRender);
        Assert.Equal(25, cut.Instance.PageSizeOnFirstRender);
    }

    [Fact]
    public void TryUpdateUrlFromState_keeps_bound_page_in_sync_with_the_table()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/workflows/instances?page=1&pageSize=10");
        var cut = Render<TestQueryTable>();

        cut.Instance.Sync(new TableState { Page = 2, PageSize = 10 });

        Assert.Equal(2, cut.Instance.Page);
        Assert.Equal(10, cut.Instance.PageSize);
        Assert.Contains("page=3", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void InstanceList_loads_the_url_page_and_does_not_rewrite_it_to_page_one()
    {
        AssertListKeepsUrlPage<WorkflowInstanceList>("/workflows/instances?page=2&pageSize=10", PageTwoInstanceId, _instances.RequestedPages);
    }

    [Fact]
    public void DefinitionList_loads_the_url_page_and_does_not_rewrite_it_to_page_one()
    {
        AssertListKeepsUrlPage<WorkflowDefinitionList>("/workflows/definitions?page=2&pageSize=10", PageTwoDefinitionName, _definitions.RequestedPages);
    }

    [Fact]
    public void InstanceList_keeps_the_current_page_when_a_row_is_selected()
    {
        AssertListKeepsPageAfterSelection<WorkflowInstanceList>("/workflows/instances?page=2&pageSize=10", PageTwoInstanceId, _instances.RequestedPages);
    }

    [Fact]
    public void DefinitionList_keeps_the_current_page_when_a_row_is_selected()
    {
        AssertListKeepsPageAfterSelection<WorkflowDefinitionList>("/workflows/definitions?page=2&pageSize=10", PageTwoDefinitionName, _definitions.RequestedPages);
    }

    private void AssertListKeepsUrlPage<TList>(string url, string expectedRowText, IReadOnlyList<int> requestedPages) where TList : IComponent
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(url);
        var cut = RenderList<TList>(expectedRowText);

        Assert.Contains("page=2", Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Equal([1], requestedPages);
        Assert.Contains(expectedRowText, cut.Markup);
    }

    [Fact]
    public void InstanceList_keeps_the_current_page_after_paging_then_selecting()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/workflows/instances");
        var cut = RenderList<WorkflowInstanceList>("instance-p0-0");

        cut.Find("button[aria-label='Next page']").Click();
        cut.WaitForAssertion(() => Assert.Contains(PageTwoInstanceId, cut.Markup));

        cut.Find("tbody input[type=checkbox]").Change(true);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("page=2", Services.GetRequiredService<NavigationManager>().Uri);
            Assert.Contains(PageTwoInstanceId, cut.Markup);
        });
        Assert.Equal([0, 1], _instances.RequestedPages);
    }

    [Fact]
    public void InstanceList_changing_page_size_resets_to_page_one()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/workflows/instances?page=2&pageSize=10");
        var cut = RenderList<WorkflowInstanceList>(PageTwoInstanceId);

        cut.Find(".mud-table-pagination-select").MouseDown();
        _popovers.WaitForAssertion(() => Assert.Contains("25", _popovers.Markup));
        _popovers.FindAll(".mud-list-item").Single(item => item.TextContent.Trim() == "25").Click();

        cut.WaitForAssertion(() =>
        {
            var uri = Services.GetRequiredService<NavigationManager>().Uri;
            Assert.Contains("page=1&pageSize=25", uri);
            Assert.Contains("instance-p0-0", cut.Markup);
        });
        Assert.Equal([1, 0], _instances.RequestedPages);
    }

    private void AssertListKeepsPageAfterSelection<TList>(string url, string expectedRowText, List<int> requestedPages) where TList : IComponent
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(url);
        var cut = RenderList<TList>(expectedRowText);

        cut.Find("tbody input[type=checkbox]").Change(true);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("page=2", Services.GetRequiredService<NavigationManager>().Uri);
            Assert.Contains(expectedRowText, cut.Markup);
        });
        Assert.Equal([1], requestedPages);
    }

    private IRenderedComponent<TList> RenderList<TList>(string expectedRowText) where TList : IComponent
    {
        var cut = Render<TList>(parameters => parameters.AddCascadingValue(StubPermissionService.Grants("workflows/*:*")));
        cut.WaitForAssertion(() => Assert.Contains(expectedRowText, cut.Markup));
        return cut;
    }

    private IWorkflowInstanceService CreateInstanceService()
    {
        var service = DispatchProxy.Create<IWorkflowInstanceService, WorkflowInstanceServiceProxy>();
        ((WorkflowInstanceServiceProxy)(object)service).RequestedPages = _instances.RequestedPages;
        return service;
    }

    private IWorkflowDefinitionService CreateDefinitionService()
    {
        var service = DispatchProxy.Create<IWorkflowDefinitionService, WorkflowDefinitionServiceProxy>();
        ((WorkflowDefinitionServiceProxy)(object)service).RequestedPages = _definitions.RequestedPages;
        return service;
    }

    private sealed class TestQueryTable : QueryTableComponentBase
    {
        public int Page => InitialPage;
        public int PageSize => InitialPageSize;
        public int PageOnFirstRender { get; private set; }
        public int PageSizeOnFirstRender { get; private set; }
        private bool _capturedFirstRender;

        public void Sync(TableState state) => TryUpdateUrlFromState(state);

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (_capturedFirstRender)
                return;

            PageOnFirstRender = InitialPage;
            PageSizeOnFirstRender = InitialPageSize;
            _capturedFirstRender = true;
        }

        protected override Task ApplyQueryParameters(IDictionary<string, string> query)
        {
            ApplyPagingFromQuery(query);
            return Task.CompletedTask;
        }

        protected override Dictionary<string, string?> BuildQueryFromState(TableState state) => new(StringComparer.OrdinalIgnoreCase)
        {
            ["page"] = (state.Page + 1).ToString(),
            ["pageSize"] = state.PageSize.ToString()
        };
    }

    public class WorkflowInstanceServiceProxy : DispatchProxy
    {
        public List<int> RequestedPages { get; set; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name != nameof(IWorkflowInstanceService.ListAsync))
                throw new InvalidOperationException($"Unexpected call to {targetMethod.DeclaringType!.Name}.{targetMethod.Name}.");

            var request = (ListWorkflowInstancesRequest)args![0]!;
            var page = request.Page ?? 0;
            RequestedPages.Add(page);

            return Task.FromResult(new PagedListResponse<WorkflowInstanceSummary>
            {
                Items =
                [
                    new()
                    {
                        Id = $"instance-p{page}-0",
                        DefinitionId = "order-process",
                        DefinitionVersionId = "order-process:1",
                        Version = 1,
                        Name = $"Instance {page}",
                        Status = WorkflowStatus.Running,
                        SubStatus = WorkflowSubStatus.Suspended
                    }
                ],
                TotalCount = 25
            });
        }
    }

    public class WorkflowDefinitionServiceProxy : DispatchProxy
    {
        private static readonly Link[] PublishLinks = [new("/publish", "publish", "POST"), new("/bulk-publish", "bulk-publish", "POST")];

        public List<int> RequestedPages { get; set; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name != nameof(IWorkflowDefinitionService.ListAsync))
                throw new InvalidOperationException($"Unexpected call to {targetMethod.DeclaringType!.Name}.{targetMethod.Name}.");

            var request = (ListWorkflowDefinitionsRequest)args![0]!;
            if (request.Ids is { Count: > 0 })
            {
                return Task.FromResult(new PagedListResponse<WorkflowDefinitionSummary>
                {
                    Items = request.Ids.Select(id => new WorkflowDefinitionSummary
                    {
                        Id = id,
                        DefinitionId = "order-process",
                        Name = "Order process",
                        Version = 1,
                        IsLatest = true,
                        Links = PublishLinks
                    }).ToList(),
                    TotalCount = request.Ids.Count,
                    Links = PublishLinks
                });
            }

            if (request.Page is { } page)
                RequestedPages.Add(page);

            var name = request.Page == 1 ? PageTwoDefinitionName : "Order process";
            return Task.FromResult(new PagedListResponse<WorkflowDefinitionSummary>
            {
                Items =
                [
                    new()
                    {
                        Id = $"order-process:{request.Page ?? 0}",
                        DefinitionId = "order-process",
                        Name = name,
                        Version = 1,
                        IsPublished = true,
                        IsLatest = true,
                        Links = PublishLinks
                    }
                ],
                TotalCount = 25,
                Links = PublishLinks
            });
        }
    }
}

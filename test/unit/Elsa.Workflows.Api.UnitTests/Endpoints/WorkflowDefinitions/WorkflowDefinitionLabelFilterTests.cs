using System.Security.Claims;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Models;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.List;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Stores;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;
namespace Elsa.Workflows.Api.UnitTests.Endpoints.WorkflowDefinitions;

public class WorkflowDefinitionLabelFilterTests
{
    [Test]
    public async Task List_WithLabelId_ReturnsMatchingVersionsAndTotalCount()
    {
        var response = await ExecuteAsync(["red"], page: 0, pageSize: 1);

        await Assert.That(response.TotalCount).IsEqualTo(2);
        var item = await Assert.That(response.Items).HasSingleItem();
        await Assert.That(new[] { "red-version", "red-second-version" }).Contains(item.Id);

        var secondPage = await ExecuteAsync(["red"], page: 1, pageSize: 1);
        await Assert.That(secondPage.TotalCount).IsEqualTo(2);
        await Assert.That(secondPage.Items).HasSingleItem();
        await Assert.That(secondPage.Items.Single().Id).IsNotEqualTo(item.Id);
        await Assert.That(new[] { "red-version", "red-second-version" }).Contains(secondPage.Items.Single().Id);
    }

    [Test]
    public async Task List_WithUnknownLabelId_ReturnsNoResults()
    {
        var response = await ExecuteAsync(["unknown"]);

        await Assert.That(response.Items).IsEmpty();
        await Assert.That(response.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task List_WithoutLabelIds_ReturnsAllVersions()
    {
        var response = await ExecuteAsync(null, permissions: ["workflows/definitions:view"]);

        await Assert.That(response.TotalCount).IsEqualTo(5);
        await Assert.That(response.Items.Count).IsEqualTo(5);
    }

    [Test]
    public async Task List_WithWorkflowPermissionWildcard_ReturnsMatchingVersions()
    {
        var response = await ExecuteAsync(["red"], permissions: ["workflows/definitions/*:view"]);

        await Assert.That(response.TotalCount).IsEqualTo(2);
        await Assert.That(response.Items.Count).IsEqualTo(2);
    }

    [Test]
    public async Task List_WithMultipleLabelIds_UsesAnyMatchingLabel()
    {
        var response = await ExecuteAsync(["red", "blue"]);

        await Assert.That(response.TotalCount).IsEqualTo(3);
        await Assert.That(response.Items.Select(x => x.Id).Order()).IsEquivalentTo(
            ["blue-version", "red-second-version", "red-version"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task List_WithLabelIdsAndVersionIds_IntersectsBothFilters()
    {
        var response = await ExecuteAsync(["red"], ids: ["red-unlabeled-version", "red-version"]);

        await Assert.That(response.TotalCount).IsEqualTo(1);
        var item = await Assert.That(response.Items).HasSingleItem();
        await Assert.That(item.Id).IsEqualTo("red-version");
    }

    [Test]
    public async Task List_WithLabelFilter_RequiresLabelPermissionBeforeQuerying()
    {
        var workflowDefinitionStore = Substitute.For<IWorkflowDefinitionStore>();
        ConfigureEmptyPage(workflowDefinitionStore);
        var labelStore = Substitute.For<IWorkflowDefinitionLabelStore, IWorkflowDefinitionLabelQuery>();
        var labelProvider = new WorkflowDefinitionLabelFilterProvider(labelStore);
        var endpoint = Factory.Create<List>(
            CreateHttpContext("workflows/definitions:view"),
            workflowDefinitionStore,
            new TestWorkflowDefinitionLinker(),
            new IWorkflowDefinitionFilterProvider[] { labelProvider });

        await endpoint.ExecuteAsync(new Request { Labels = ["red"] }, CancellationToken.None);

        await Assert.That(endpoint.HttpContext.Response.StatusCode).IsEqualTo(StatusCodes.Status403Forbidden);
        await Assert.That(labelStore.ReceivedCalls()).IsEmpty();
        await Assert.That(workflowDefinitionStore.ReceivedCalls()).IsEmpty();
    }

    [Test]
    public async Task List_WithUnrelatedApplicableProvider_LeavesLabelFilterUnsupported()
    {
        var workflowDefinitionStore = Substitute.For<IWorkflowDefinitionStore>();
        ConfigureEmptyPage(workflowDefinitionStore);
        var endpoint = Factory.Create<List>(
            CreateHttpContext("workflows/definitions:view", "workflows/definitions/labels:view"),
            workflowDefinitionStore,
            new TestWorkflowDefinitionLinker(),
            new IWorkflowDefinitionFilterProvider[] { new UnrelatedFilterProvider() });

        await endpoint.ExecuteAsync(new Request { Labels = ["red"] }, CancellationToken.None);

        await Assert.That(endpoint.HttpContext.Response.StatusCode).IsEqualTo(StatusCodes.Status501NotImplemented);
        await Assert.That(workflowDefinitionStore.ReceivedCalls()).IsEmpty();
    }

    [Test]
    public async Task LabelFilterProvider_WithUnsupportedStoreThrowsExplicitly()
    {
        var provider = new WorkflowDefinitionLabelFilterProvider(Substitute.For<IWorkflowDefinitionLabelStore>());
        var filter = new WorkflowDefinitionFilter { LabelIds = ["red"] };

        await Assert.ThrowsExactlyAsync<WorkflowDefinitionFilterNotSupportedException>(() => provider.ApplyAsync(filter));
    }

    [Test]
    public async Task List_WithoutLabelProvider_ReturnsNotImplemented()
    {
        var store = Substitute.For<IWorkflowDefinitionStore>();
        var endpoint = Factory.Create<List>(new DefaultHttpContext(), store, new TestWorkflowDefinitionLinker(), Array.Empty<IWorkflowDefinitionFilterProvider>());

        await endpoint.ExecuteAsync(new Request { Labels = ["red"] }, CancellationToken.None);

        await Assert.That(endpoint.HttpContext.Response.StatusCode).IsEqualTo(StatusCodes.Status501NotImplemented);
        await Assert.That(store.ReceivedCalls()).IsEmpty();
    }

    [Test]
    public async Task List_WithUnsupportedLabelStore_ReturnsNotImplemented()
    {
        var store = Substitute.For<IWorkflowDefinitionStore>();
        var labelStore = Substitute.For<IWorkflowDefinitionLabelStore>();
        var provider = new WorkflowDefinitionLabelFilterProvider(labelStore);
        var endpoint = Factory.Create<List>(
            CreateHttpContext("workflows/definitions:view", "workflows/definitions/labels:view"),
            store,
            new TestWorkflowDefinitionLinker(),
            new IWorkflowDefinitionFilterProvider[] { provider });

        await endpoint.ExecuteAsync(new Request { Labels = ["red"] }, CancellationToken.None);

        await Assert.That(endpoint.HttpContext.Response.StatusCode).IsEqualTo(StatusCodes.Status501NotImplemented);
        await Assert.That(store.ReceivedCalls()).IsEmpty();
    }

    private static async Task<PagedListResponse<LinkedWorkflowDefinitionSummary>> ExecuteAsync(string[]? labels, string[]? ids = null, int? page = 0, int? pageSize = null, string[]? permissions = null)
    {
        var memoryStore = new MemoryStore<WorkflowDefinition>();
        var workflowDefinitionStore = new MemoryWorkflowDefinitionStore(memoryStore);
        await workflowDefinitionStore.SaveManyAsync(
        [
            new WorkflowDefinition { Id = "red-version", DefinitionId = "red", Name = "Red", MaterializerName = "Json", Version = 1 },
            new WorkflowDefinition { Id = "red-unlabeled-version", DefinitionId = "red", Name = "Red", MaterializerName = "Json", Version = 2 },
            new WorkflowDefinition { Id = "red-second-version", DefinitionId = "red-second", Name = "Red second", MaterializerName = "Json" },
            new WorkflowDefinition { Id = "blue-version", DefinitionId = "blue", Name = "Blue", MaterializerName = "Json" },
            new WorkflowDefinition { Id = "plain-version", DefinitionId = "plain", Name = "Plain", MaterializerName = "Json" }
        ]);

        var labelStore = new InMemoryWorkflowDefinitionLabelStore(new MemoryStore<WorkflowDefinitionLabel>());
        await labelStore.SaveManyAsync(
        [
            new WorkflowDefinitionLabel { Id = "red-association", WorkflowDefinitionId = "red", WorkflowDefinitionVersionId = "red-version", LabelId = "red" },
            new WorkflowDefinitionLabel { Id = "red-second-association", WorkflowDefinitionId = "red-second", WorkflowDefinitionVersionId = "red-second-version", LabelId = "red" },
            new WorkflowDefinitionLabel { Id = "blue-association", WorkflowDefinitionId = "blue", WorkflowDefinitionVersionId = "blue-version", LabelId = "blue" }
        ]);

        var endpoint = Factory.Create<List>(
            CreateHttpContext(permissions ?? ["workflows/definitions:view", "workflows/definitions/labels:view"]),
            workflowDefinitionStore,
            new TestWorkflowDefinitionLinker(),
            new IWorkflowDefinitionFilterProvider[] { new WorkflowDefinitionLabelFilterProvider(labelStore) });

        return await endpoint.ExecuteAsync(new Request { Labels = labels, Ids = ids, Page = page, PageSize = pageSize }, CancellationToken.None);
    }

    private sealed class TestWorkflowDefinitionLinker : IWorkflowDefinitionLinker
    {
        public Task<LinkedWorkflowDefinitionModel> MapAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public PagedListResponse<LinkedWorkflowDefinitionSummary> MapAsync(PagedListResponse<WorkflowDefinitionSummary> list, CancellationToken cancellationToken = default) => new()
        {
            Items = list.Items.Select(x => new LinkedWorkflowDefinitionSummary
            {
                Id = x.Id,
                DefinitionId = x.DefinitionId,
                Name = x.Name,
                Version = x.Version
            }).ToList(),
            TotalCount = list.TotalCount
        };

        public Task<List<LinkedWorkflowDefinitionModel>> MapAsync(List<WorkflowDefinition> definitions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnrelatedFilterProvider : IWorkflowDefinitionFilterProvider
    {
        public bool CanApply(WorkflowDefinitionFilter filter) => true;

        public Task ApplyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static DefaultHttpContext CreateHttpContext(params string[] permissions) => new()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(permissions.Select(x => new Claim(PermissionNames.ClaimType, x)), "test"))
    };

    private static void ConfigureEmptyPage(IWorkflowDefinitionStore store) =>
        store.FindSummariesAsync(
                Arg.Any<WorkflowDefinitionFilter>(),
                Arg.Any<WorkflowDefinitionOrder<string>>(),
                Arg.Any<PageArgs>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Page.Empty<WorkflowDefinitionSummary>()));
}

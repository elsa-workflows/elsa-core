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
    [Fact]
    public async Task List_WithLabelId_ReturnsMatchingVersionsAndTotalCount()
    {
        var response = await ExecuteAsync(["red"], page: 0, pageSize: 1);

        Assert.Equal(2, response.TotalCount);
        var item = Assert.Single(response.Items);
        Assert.Contains(item.Id, new[] { "red-version", "red-second-version" });

        var secondPage = await ExecuteAsync(["red"], page: 1, pageSize: 1);
        Assert.Equal(2, secondPage.TotalCount);
        Assert.Single(secondPage.Items);
        Assert.NotEqual(item.Id, secondPage.Items.Single().Id);
        Assert.Contains(secondPage.Items.Single().Id, new[] { "red-version", "red-second-version" });
    }

    [Fact]
    public async Task List_WithUnknownLabelId_ReturnsNoResults()
    {
        var response = await ExecuteAsync(["unknown"]);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public async Task List_WithoutLabelIds_ReturnsAllVersions()
    {
        var response = await ExecuteAsync(null);

        Assert.Equal(5, response.TotalCount);
        Assert.Equal(5, response.Items.Count);
    }

    [Fact]
    public async Task List_WithMultipleLabelIds_UsesAnyMatchingLabel()
    {
        var response = await ExecuteAsync(["red", "blue"]);

        Assert.Equal(3, response.TotalCount);
        Assert.Equal(["blue-version", "red-second-version", "red-version"], response.Items.Select(x => x.Id).Order());
    }

    [Fact]
    public async Task List_WithLabelIdsAndVersionIds_IntersectsBothFilters()
    {
        var response = await ExecuteAsync(["red"], ids: ["red-unlabeled-version", "red-version"]);

        Assert.Equal(1, response.TotalCount);
        Assert.Equal("red-version", Assert.Single(response.Items).Id);
    }

    [Fact]
    public async Task LabelFilterProvider_WithUnsupportedStoreThrowsExplicitly()
    {
        var provider = new WorkflowDefinitionLabelFilterProvider(Substitute.For<IWorkflowDefinitionLabelStore>());
        var filter = new WorkflowDefinitionFilter { LabelIds = ["red"] };

        await Assert.ThrowsAsync<WorkflowDefinitionFilterNotSupportedException>(() => provider.ApplyAsync(filter));
    }

    [Fact]
    public async Task List_WithoutLabelProvider_ReturnsNotImplemented()
    {
        var store = Substitute.For<IWorkflowDefinitionStore>();
        var endpoint = Factory.Create<List>(new DefaultHttpContext(), store, new TestWorkflowDefinitionLinker(), Array.Empty<IWorkflowDefinitionFilterProvider>());

        await endpoint.ExecuteAsync(new Request { Labels = ["red"] }, CancellationToken.None);

        Assert.Equal(StatusCodes.Status501NotImplemented, endpoint.HttpContext.Response.StatusCode);
        Assert.Empty(store.ReceivedCalls());
    }

    [Fact]
    public async Task List_WithUnsupportedLabelStore_ReturnsNotImplemented()
    {
        var store = Substitute.For<IWorkflowDefinitionStore>();
        var labelStore = Substitute.For<IWorkflowDefinitionLabelStore>();
        var provider = new WorkflowDefinitionLabelFilterProvider(labelStore);
        var endpoint = Factory.Create<List>(new DefaultHttpContext(), store, new TestWorkflowDefinitionLinker(), new IWorkflowDefinitionFilterProvider[] { provider });

        await endpoint.ExecuteAsync(new Request { Labels = ["red"] }, CancellationToken.None);

        Assert.Equal(StatusCodes.Status501NotImplemented, endpoint.HttpContext.Response.StatusCode);
        Assert.Empty(store.ReceivedCalls());
    }

    private static async Task<PagedListResponse<LinkedWorkflowDefinitionSummary>> ExecuteAsync(string[]? labels, string[]? ids = null, int? page = 0, int? pageSize = null)
    {
        var memoryStore = new MemoryStore<WorkflowDefinition>();
        var workflowDefinitionStore = new MemoryWorkflowDefinitionStore(memoryStore);
        await workflowDefinitionStore.SaveManyAsync(
        [
            new WorkflowDefinition { Id = "red-version", DefinitionId = "red", Name = "Red", MaterializerName = "Json" },
            new WorkflowDefinition { Id = "red-unlabeled-version", DefinitionId = "red", Name = "Red", MaterializerName = "Json" },
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

        var endpoint = new List(
            workflowDefinitionStore,
            new TestWorkflowDefinitionLinker(),
            [new WorkflowDefinitionLabelFilterProvider(labelStore)]);

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
}

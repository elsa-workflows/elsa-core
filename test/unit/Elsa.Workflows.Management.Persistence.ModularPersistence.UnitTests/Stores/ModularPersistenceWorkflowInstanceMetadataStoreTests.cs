using Elsa.Common.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Enums;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Persistence.ModularPersistence.Storage;
using Elsa.Workflows.Management.Persistence.ModularPersistence.Stores;
using Elsa.Workflows.Management.Persistence.ModularPersistence.UnitTests.Testing;

namespace Elsa.Workflows.Management.Persistence.ModularPersistence.UnitTests.Stores;

public class ModularPersistenceWorkflowInstanceMetadataStoreTests
{
    private readonly InMemoryDocumentStore _documentStore = new();
    private readonly ModularPersistenceWorkflowInstanceMetadataStore _store;

    public ModularPersistenceWorkflowInstanceMetadataStoreTests()
    {
        _store = new ModularPersistenceWorkflowInstanceMetadataStore(_documentStore);
    }

    [Fact]
    public async Task SaveAsyncPersistsMetadataWithoutWorkflowState()
    {
        var instance = CreateInstance("instance-1", "tenant-a");

        await _store.SaveAsync(WorkflowInstanceMetadataRecord.FromInstance(instance));

        var document = Assert.Single(_documentStore.Documents);
        Assert.Equal(WorkflowInstanceMetadataStorageManifest.StorageUnitName, document.Type);
        Assert.Equal("tenant-a", document.TenantId);
        Assert.Contains("\"DefinitionId\":\"definition-a\"", document.Data);
        Assert.DoesNotContain("WorkflowState", document.Data);
    }

    [Fact]
    public async Task SummarizeManyAsyncUsesIndexedMetadataFilters()
    {
        await SaveFixturesAsync();

        var page = await _store.SummarizeManyAsync(
            new WorkflowInstanceFilter
            {
                DefinitionId = "definition-a",
                WorkflowStatus = WorkflowStatus.Running,
                HasIncidents = true
            },
            PageArgs.FromRange(0, 10),
            "tenant-a");

        var summary = Assert.Single(page.Items);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal("instance-1", summary.Id);
        Assert.Equal(2, summary.IncidentCount);
    }

    [Fact]
    public async Task FindManyIdsAsyncPaginatesDeclaredIndexResults()
    {
        await SaveFixturesAsync();

        var page = await _store.FindManyIdsAsync(
            new WorkflowInstanceFilter
            {
                WorkflowStatus = WorkflowStatus.Running
            },
            PageArgs.FromRange(1, 1),
            "tenant-a");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal("instance-2", Assert.Single(page.Items));
    }

    [Fact]
    public async Task CountAsyncAppliesTenantScopeAndTimestampFilter()
    {
        await SaveFixturesAsync();

        var count = await _store.CountAsync(
            new WorkflowInstanceFilter
            {
                TimestampFilters =
                [
                    new TimestampFilter
                    {
                        Column = "UpdatedAt",
                        Operator = TimestampFilterOperator.GreaterThanOrEqual,
                        Timestamp = new DateTimeOffset(2026, 01, 02, 0, 0, 0, TimeSpan.Zero)
                    }
                ]
            },
            "tenant-a");

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountAsyncWithoutFiltersUsesIdIndexFallback()
    {
        await SaveFixturesAsync();

        var count = await _store.CountAsync(new WorkflowInstanceFilter(), "tenant-a");

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task FindAsyncLoadsSingleMetadataRecordByTenant()
    {
        await SaveFixturesAsync();

        var found = await _store.FindAsync("instance-1", "tenant-a");
        var wrongTenant = await _store.FindAsync("instance-1", "tenant-c");

        Assert.NotNull(found);
        Assert.Null(wrongTenant);
        Assert.Equal("definition-a", found.DefinitionId);
    }

    [Fact]
    public async Task QueryRejectsSearchTermBecauseItWouldRequireScanOrProviderSpecificTextSearch()
    {
        await SaveFixturesAsync();

        var exception = await Assert.ThrowsAsync<WorkflowInstanceMetadataQueryException>(async () =>
            await _store.CountAsync(new WorkflowInstanceFilter { SearchTerm = "definition" }));

        Assert.Contains("SearchTerm", exception.Message);
    }

    [Fact]
    public async Task QueryRejectsContainsNameFilterButAllowsExactNames()
    {
        await SaveFixturesAsync();

        await Assert.ThrowsAsync<WorkflowInstanceMetadataQueryException>(async () =>
            await _store.CountAsync(new WorkflowInstanceFilter { Name = "Import" }));

        var page = await _store.FindManyIdsAsync(
            new WorkflowInstanceFilter { Names = ["Import Orders"] },
            PageArgs.FromRange(0, 10),
            "tenant-a");

        Assert.Equal("instance-1", Assert.Single(page.Items));
    }

    private async Task SaveFixturesAsync()
    {
        var records = new[]
        {
            WorkflowInstanceMetadataRecord.FromInstance(CreateInstance("instance-1", "tenant-a", "definition-a", WorkflowStatus.Running, WorkflowSubStatus.Suspended, 2, "Import Orders", 1)),
            WorkflowInstanceMetadataRecord.FromInstance(CreateInstance("instance-2", "tenant-a", "definition-a", WorkflowStatus.Running, WorkflowSubStatus.Pending, 0, "Archive Orders", 2)),
            WorkflowInstanceMetadataRecord.FromInstance(CreateInstance("instance-3", "tenant-a", "definition-b", WorkflowStatus.Finished, WorkflowSubStatus.Finished, 0, "Import Customers", 3)),
            WorkflowInstanceMetadataRecord.FromInstance(CreateInstance("instance-1", "tenant-b", "definition-a", WorkflowStatus.Running, WorkflowSubStatus.Suspended, 1, "Other Tenant", 4))
        };

        await _store.SaveManyAsync(records);
    }

    private static WorkflowInstance CreateInstance(
        string id,
        string? tenantId,
        string definitionId = "definition-a",
        WorkflowStatus status = WorkflowStatus.Running,
        WorkflowSubStatus subStatus = WorkflowSubStatus.Pending,
        int incidentCount = 0,
        string name = "Import Orders",
        int day = 1) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            DefinitionId = definitionId,
            DefinitionVersionId = $"{definitionId}:v1",
            Version = 1,
            ParentWorkflowInstanceId = "parent-1",
            Status = status,
            SubStatus = subStatus,
            IsExecuting = status == WorkflowStatus.Running,
            CorrelationId = $"correlation-{id}",
            Name = name,
            IncidentCount = incidentCount,
            IsSystem = false,
            CreatedAt = new DateTimeOffset(2026, 01, day, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 01, day, 12, 0, 0, TimeSpan.Zero),
            FinishedAt = status == WorkflowStatus.Finished ? new DateTimeOffset(2026, 01, day, 13, 0, 0, TimeSpan.Zero) : null
        };
}

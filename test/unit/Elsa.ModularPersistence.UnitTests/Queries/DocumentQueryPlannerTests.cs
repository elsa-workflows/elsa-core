using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Queries;

namespace Elsa.ModularPersistence.UnitTests.Queries;

public class DocumentQueryPlannerTests
{
    private readonly StorageManifestDescriptor _manifest = CreateWorkflowDefinitionManifest();
    private readonly DocumentQueryPlanner _planner = new();

    [Fact]
    public void CanPlanWorkflowDefinitionLookupByDefinitionIdAndVersion()
    {
        var query = new DocumentQuery(
            "WorkflowDefinitions",
            [
                DocumentQueryFilter.Equal("IX_WorkflowDefinitions_DefinitionId", "DefinitionId", DocumentQueryValue.String("definition-1")),
                DocumentQueryFilter.Equal("IX_WorkflowDefinitions_Version", "Version", DocumentQueryValue.Int32(3))
            ],
            page: new DocumentQueryPage(1));

        var plan = _planner.Plan(_manifest, query);

        Assert.True(plan.IsExecutable);
        Assert.Empty(plan.Diagnostics);
        Assert.Equal("WorkflowDefinitions", plan.StorageUnit?.Name);
        Assert.Equal(["IX_WorkflowDefinitions_DefinitionId", "IX_WorkflowDefinitions_Version"], plan.ReferencedIndexes.Select(x => x.Name).Order().ToArray());
    }

    [Fact]
    public void CanPlanWorkflowDefinitionListByTenantLatestAndNamePrefixWhenProviderSupportsStartsWith()
    {
        var query = new DocumentQuery(
            "WorkflowDefinitions",
            [
                DocumentQueryFilter.Equal("IX_WorkflowDefinitions_TenantId", "TenantId", DocumentQueryValue.String("tenant-a")),
                DocumentQueryFilter.Equal("IX_WorkflowDefinitions_IsLatest", "IsLatest", DocumentQueryValue.Boolean(true)),
                DocumentQueryFilter.StartsWith("IX_WorkflowDefinitions_Name", "Name", DocumentQueryValue.String("Import"))
            ],
            [
                new DocumentQuerySort("IX_WorkflowDefinitions_Name", "Name")
            ],
            new DocumentQueryPage(50));

        var plan = _planner.Plan(_manifest, query, DocumentQueryCapabilities.WithStartsWith);

        Assert.True(plan.IsExecutable);
        Assert.Empty(plan.Diagnostics);
        Assert.Equal(3, plan.ReferencedIndexes.Count);
    }

    [Fact]
    public void ReportsUnboundedQuery()
    {
        var query = new DocumentQuery("WorkflowDefinitions", page: new DocumentQueryPage(50));

        var plan = _planner.Plan(_manifest, query);

        var diagnostic = Assert.Single(plan.Diagnostics);
        Assert.False(plan.IsExecutable);
        Assert.Equal("UnboundedQuery", diagnostic.Code);
        Assert.Equal("filters", diagnostic.Path);
    }

    [Fact]
    public void ReportsUnknownIndex()
    {
        var query = new DocumentQuery(
            "WorkflowDefinitions",
            [
                DocumentQueryFilter.Equal("IX_WorkflowDefinitions_Missing", "Name", DocumentQueryValue.String("Import"))
            ]);

        var plan = _planner.Plan(_manifest, query);

        var diagnostic = Assert.Single(plan.Diagnostics);
        Assert.Equal("UnknownIndex", diagnostic.Code);
        Assert.Equal("filters[0].indexName", diagnostic.Path);
    }

    [Fact]
    public void ReportsFieldNotDeclaredByIndex()
    {
        var query = new DocumentQuery(
            "WorkflowDefinitions",
            [
                DocumentQueryFilter.Equal("IX_WorkflowDefinitions_Name", "Version", DocumentQueryValue.Int32(1))
            ]);

        var plan = _planner.Plan(_manifest, query);

        var diagnostic = Assert.Single(plan.Diagnostics);
        Assert.Equal("FieldNotInIndex", diagnostic.Code);
        Assert.Equal("filters[0].indexName", diagnostic.Path);
    }

    [Fact]
    public void ReportsUnsupportedStartsWithByDefault()
    {
        var query = new DocumentQuery(
            "WorkflowDefinitions",
            [
                DocumentQueryFilter.StartsWith("IX_WorkflowDefinitions_Name", "Name", DocumentQueryValue.String("Import"))
            ]);

        var plan = _planner.Plan(_manifest, query);

        var diagnostic = Assert.Single(plan.Diagnostics);
        Assert.Equal("UnsupportedQueryOperator", diagnostic.Code);
        Assert.Equal("filters[0].operator", diagnostic.Path);
    }

    [Fact]
    public void ReportsQueryValueTypeMismatch()
    {
        var query = new DocumentQuery(
            "WorkflowDefinitions",
            [
                DocumentQueryFilter.Equal("IX_WorkflowDefinitions_Version", "Version", DocumentQueryValue.String("3"))
            ]);

        var plan = _planner.Plan(_manifest, query);

        var diagnostic = Assert.Single(plan.Diagnostics);
        Assert.Equal("QueryValueTypeMismatch", diagnostic.Code);
        Assert.Equal("filters[0].values[0]", diagnostic.Path);
    }

    [Fact]
    public void FilterConstructorsValidateValueCounts()
    {
        var inException = Assert.Throws<ArgumentException>(() => DocumentQueryFilter.In("IX_WorkflowDefinitions_Name", "Name", []));
        var nullException = Assert.Throws<ArgumentException>(() => new DocumentQueryFilter("IX_WorkflowDefinitions_Name", "Name", DocumentQueryFilterOperator.IsNull, [DocumentQueryValue.String("x")]));

        Assert.Equal("Values", inException.ParamName);
        Assert.Equal("Values", nullException.ParamName);
    }

    [Fact]
    public void PageRequiresPositiveLimitAndNonNegativeOffset()
    {
        Assert.Equal("limit", Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentQueryPage(0)).ParamName);
        Assert.Equal("offset", Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentQueryPage(10, -1)).ParamName);
    }

    private static StorageManifestDescriptor CreateWorkflowDefinitionManifest() =>
        new(
            "elsa.workflow-definitions",
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    "WorkflowDefinitions",
                    [
                        new StorageFieldDescriptor("Id", StorageFieldType.String, true),
                        new StorageFieldDescriptor("DefinitionId", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Version", StorageFieldType.Int32, true),
                        new StorageFieldDescriptor("Name", StorageFieldType.String),
                        new StorageFieldDescriptor("TenantId", StorageFieldType.String),
                        new StorageFieldDescriptor("IsLatest", StorageFieldType.Boolean)
                    ],
                    [
                        new StorageKeyDescriptor("PK_WorkflowDefinitions", ["Id"])
                    ],
                    [
                        new StorageIndexDescriptor("IX_WorkflowDefinitions_DefinitionId", [new StorageIndexFieldDescriptor("DefinitionId")]),
                        new StorageIndexDescriptor("IX_WorkflowDefinitions_Version", [new StorageIndexFieldDescriptor("Version")]),
                        new StorageIndexDescriptor("IX_WorkflowDefinitions_Name", [new StorageIndexFieldDescriptor("Name")]),
                        new StorageIndexDescriptor("IX_WorkflowDefinitions_TenantId", [new StorageIndexFieldDescriptor("TenantId")]),
                        new StorageIndexDescriptor("IX_WorkflowDefinitions_IsLatest", [new StorageIndexFieldDescriptor("IsLatest")])
                    ])
            ]);
}

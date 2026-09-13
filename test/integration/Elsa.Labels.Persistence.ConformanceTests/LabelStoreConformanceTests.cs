using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Labels.Entities;

namespace Elsa.Labels.Persistence.ConformanceTests;

/// <summary>
/// Shared InMemory / EF Core store-contract assertions for Labels ports.
/// </summary>
public abstract class LabelStoreConformanceTests
{
    protected abstract Task<LabelStoreScenario> CreateScenarioAsync();

    [Fact]
    public async Task DeletingALabelCascadesItsAssociations()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.Labels.SaveAsync(Label("label-a", "A", "tenant-a"));
        await scenario.Labels.SaveAsync(Label("label-keep", "Keep", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-a1", "label-a", "order", "order:1", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-a2", "label-a", "invoice", "invoice:1", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-keep", "label-keep", "order", "order:1", "tenant-a"));

        using (scenario.UseTenant("tenant-b"))
        {
            await scenario.Labels.SaveAsync(Label("label-b", "A", "tenant-b"));
            await scenario.Associations.SaveAsync(Association("assoc-b", "label-b", "order", "order:1", "tenant-b"));
        }

        Assert.True(await scenario.Labels.DeleteAsync("label-a"));
        Assert.Empty(await scenario.AssociationQuery.FindByLabelIdsAsync(["label-a"]));
        Assert.Equal("assoc-keep", Assert.Single(await scenario.AssociationQuery.FindByLabelIdsAsync(["label-keep"])).Id);
        using (scenario.UseTenant("tenant-b"))
        {
            Assert.NotNull(await scenario.Labels.FindByIdAsync("label-b"));
            Assert.Equal("assoc-b", Assert.Single(await scenario.AssociationQuery.FindByLabelIdsAsync(["label-b"])).Id);
        }

        await scenario.Labels.SaveAsync(Label("label-x", "X", "tenant-a"));
        await scenario.Labels.SaveAsync(Label("label-y", "Y", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-x", "label-x", "order", "order:2", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-y", "label-y", "order", "order:2", "tenant-a"));

        Assert.Equal(2, await scenario.Labels.DeleteManyAsync(["label-x", "label-y"]));
        Assert.Empty(await scenario.AssociationQuery.FindByLabelIdsAsync(["label-x", "label-y"]));
        Assert.Equal("assoc-keep", Assert.Single(await scenario.AssociationQuery.FindByLabelIdsAsync(["label-keep"])).Id);
    }

    [Fact]
    public async Task ReplaceAsyncRemovesAndAddsByAssociationId()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.Associations.SaveAsync(Association("assoc-red", "red", "order", "order:1", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-blue", "blue", "order", "order:1", "tenant-a"));

        await scenario.Associations.ReplaceAsync(
            [Association("assoc-red", "wrong-label", "other", "other:9", "tenant-b")],
            [Association("assoc-green", "green", "order", "order:1", "tenant-a")]);

        var remaining = (await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1"))
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToList();
        Assert.Equal(["assoc-blue", "assoc-green"], remaining);
    }

    [Fact]
    public async Task AssociationFindsAndDeletesHonorWorkflowDefinitionKeys()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.Associations.SaveAsync(Association("assoc-v1-red", "red", "order", "order:1", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-v1-blue", "blue", "order", "order:1", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-v2-red", "red", "order", "order:2", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-invoice", "red", "invoice", "invoice:1", "tenant-a"));

        var version1 = (await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();
        Assert.Equal(2, version1.Count);
        Assert.Contains(version1, x => x.Id == "assoc-v1-red");
        Assert.Contains(version1, x => x.Id == "assoc-v1-blue");
        Assert.Empty(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("missing"));

        Assert.Empty(await scenario.AssociationQuery.FindByLabelIdsAsync([]));
        Assert.Empty(await scenario.AssociationQuery.FindByLabelIdsAsync(["unknown"]));
        var byRed = (await scenario.AssociationQuery.FindByLabelIdsAsync(["red"])).ToList();
        Assert.Equal(3, byRed.Count);
        Assert.Contains(byRed, x => x.Id == "assoc-v1-red");
        Assert.Contains(byRed, x => x.Id == "assoc-v2-red");
        Assert.Contains(byRed, x => x.Id == "assoc-invoice");

        var byRedAndBlue = (await scenario.AssociationQuery.FindByLabelIdsAsync(["red", "blue"])).ToList();
        Assert.Equal(4, byRedAndBlue.Count);
        Assert.Contains(byRedAndBlue, x => x.Id == "assoc-v1-blue");

        Assert.Equal(1, await scenario.Associations.DeleteByWorkflowDefinitionVersionIdAsync("order:2"));
        Assert.Empty(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:2"));
        Assert.NotEmpty(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1"));

        Assert.Equal(1, await scenario.Associations.DeleteByWorkflowDefinitionIdAsync("invoice"));
        Assert.Empty(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("invoice:1"));

        await scenario.Associations.SaveManyAsync(
        [
            Association("assoc-pay-1", "red", "payroll", "payroll:1", "tenant-a"),
            Association("assoc-pay-2", "red", "payroll", "payroll:2", "tenant-a"),
            Association("assoc-hr-1", "blue", "hr", "hr:1", "tenant-a")
        ]);
        Assert.Equal(2, await scenario.Associations.DeleteByWorkflowDefinitionVersionIdsAsync(["payroll:1", "payroll:2"]));
        Assert.Equal(1, await scenario.Associations.DeleteByWorkflowDefinitionIdsAsync(["hr"]));
        Assert.Empty(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("payroll:1"));
        Assert.Empty(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("hr:1"));
        Assert.NotEmpty(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1"));
    }

    [Fact]
    public async Task ListAsyncOrdersByNameAndPages()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.Labels.SaveAsync(Label("label-z", "Zebra", "tenant-a"));
        await scenario.Labels.SaveAsync(Label("label-a", "Apple", "tenant-a"));
        await scenario.Labels.SaveAsync(Label("label-m", "Mango", "tenant-a"));

        var all = (await scenario.Labels.ListAsync()).Items.Select(x => x.Name).ToList();
        Assert.Equal(["Apple", "Mango", "Zebra"], all);

        // Page.TotalCount is not a shared contract: Memory ToPage counts after Skip/Take,
        // EF PaginateAsync counts the unpaged query.
        var firstPage = await scenario.Labels.ListAsync(PageArgs.FromRange(0, 2));
        Assert.Equal(["Apple", "Mango"], firstPage.Items.Select(x => x.Name).ToList());

        var secondPage = await scenario.Labels.ListAsync(PageArgs.FromRange(2, 2));
        Assert.Equal(["Zebra"], secondPage.Items.Select(x => x.Name).ToList());
    }

    [Fact]
    public async Task TenantStampAndIsolationHonorAmbientTenant()
    {
        await using var scenario = await CreateScenarioAsync();
        await SeedMixedLabelsAsync(scenario);
        await SeedMixedAssociationsAsync(scenario);

        var listed = (await scenario.Labels.ListAsync()).Items.ToList();
        Assert.Equal(2, listed.Count);
        Assert.Contains(listed, x => x.Id == "label-a");
        Assert.Contains(listed, x => x.Id == "label-star");
        Assert.DoesNotContain(listed, x => x.Id == "label-b");

        Assert.Null(await scenario.Labels.FindByIdAsync("label-b"));
        Assert.NotNull(await scenario.Labels.FindByIdAsync("label-a"));
        Assert.NotNull(await scenario.Labels.FindByIdAsync("label-star"));

        var foundMany = (await scenario.Labels.FindManyByIdAsync(["label-a", "label-b", "label-star"])).ToList();
        Assert.Equal(2, foundMany.Count);
        Assert.DoesNotContain(foundMany, x => x.Id == "label-b");

        var byVersion = (await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();
        Assert.Equal(2, byVersion.Count);
        Assert.Contains(byVersion, x => x.Id == "assoc-a");
        Assert.Contains(byVersion, x => x.Id == "assoc-star");
        Assert.DoesNotContain(byVersion, x => x.Id == "assoc-b");

        var byLabel = (await scenario.AssociationQuery.FindByLabelIdsAsync(["red"])).ToList();
        Assert.Equal(2, byLabel.Count);
        Assert.DoesNotContain(byLabel, x => x.Id == "assoc-b");

        using (scenario.UseTenant("tenant-b"))
        {
            var tenantBLabels = (await scenario.Labels.ListAsync()).Items.ToList();
            Assert.Contains(tenantBLabels, x => x.Id == "label-star");
            Assert.DoesNotContain(tenantBLabels, x => x.Id == "label-a");
            Assert.Equal("label-star", (await scenario.Labels.FindByIdAsync("label-star"))!.Id);

            var tenantBByVersion = (await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();
            Assert.Equal(2, tenantBByVersion.Count);
            Assert.Contains(tenantBByVersion, x => x.Id == "assoc-b");
            Assert.Contains(tenantBByVersion, x => x.Id == "assoc-star");
            Assert.DoesNotContain(tenantBByVersion, x => x.Id == "assoc-a");

            var tenantBByLabel = (await scenario.AssociationQuery.FindByLabelIdsAsync(["red"])).ToList();
            Assert.Equal(2, tenantBByLabel.Count);
            Assert.Contains(tenantBByLabel, x => x.Id == "assoc-b");
            Assert.Contains(tenantBByLabel, x => x.Id == "assoc-star");
            Assert.DoesNotContain(tenantBByLabel, x => x.Id == "assoc-a");
        }

        Assert.False(await scenario.Labels.DeleteAsync("label-b"));
        Assert.False(await scenario.Associations.DeleteAsync("assoc-b"));
        using (scenario.UseTenant("tenant-b"))
        {
            Assert.NotNull(await scenario.Labels.FindByIdAsync("label-b"));
            Assert.Contains(await scenario.AssociationQuery.FindByLabelIdsAsync(["red"]), x => x.Id == "assoc-b");
        }

        await scenario.Associations.ReplaceAsync(
            [Association("assoc-a", "red", "order", "order:1", "tenant-a"), Association("assoc-b", "red", "order", "order:1", "tenant-b")],
            [Association("assoc-a2", "red", "order", "order:1", "tenant-a")]);
        var remainingA = (await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();
        Assert.Contains(remainingA, x => x.Id == "assoc-a2");
        Assert.Contains(remainingA, x => x.Id == "assoc-star");
        Assert.DoesNotContain(remainingA, x => x.Id == "assoc-a");
        using (scenario.UseTenant("tenant-b"))
            Assert.Contains(await scenario.AssociationQuery.FindByLabelIdsAsync(["red"]), x => x.Id == "assoc-b");

        var deletedLabels = await scenario.Labels.DeleteManyAsync(["label-a", "label-b", "label-star"]);
        Assert.Equal(2, deletedLabels);
        using (scenario.UseTenant("tenant-b"))
            Assert.Equal("label-b", (await scenario.Labels.FindByIdAsync("label-b"))!.Id);

        using (scenario.UseTenant("tenant-b"))
            await scenario.Associations.SaveAsync(Association("assoc-b-order", "blue", "order", "order:1", "tenant-b"));
        Assert.Equal(2, await scenario.Associations.DeleteByWorkflowDefinitionIdAsync("order"));
        Assert.Empty(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1"));
        using (scenario.UseTenant("tenant-b"))
        {
            var stillB = (await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();
            Assert.Contains(stillB, x => x.Id == "assoc-b");
            Assert.Contains(stillB, x => x.Id == "assoc-b-order");
        }

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            var stampedLabel = Label("label-null", "Null", tenantId: null);
            await scenario.Labels.SaveAsync(stampedLabel);
            Assert.Equal(Tenant.DefaultTenantId, stampedLabel.TenantId);

            var namedLabel = Label("label-named", "Named", "tenant-a");
            await scenario.Labels.SaveAsync(namedLabel);
            Assert.Equal("tenant-a", namedLabel.TenantId);

            var defaultLabels = (await scenario.Labels.ListAsync()).Items.ToList();
            Assert.Contains(defaultLabels, x => x.Id == "label-null");
            Assert.DoesNotContain(defaultLabels, x => x.Id == "label-named");

            var stampedAssociation = Association("assoc-null", "red", "order", "order:1", tenantId: null);
            await scenario.Associations.SaveAsync(stampedAssociation);
            Assert.Equal(Tenant.DefaultTenantId, stampedAssociation.TenantId);
            Assert.Equal("assoc-null", Assert.Single(await scenario.AssociationQuery.FindByLabelIdsAsync(["red"])).Id);

            var batchLabels = new[] { Label("label-batch-null", "BatchNull", tenantId: null) };
            await scenario.Labels.SaveManyAsync(batchLabels);
            Assert.Equal(Tenant.DefaultTenantId, batchLabels[0].TenantId);
            Assert.Equal(Tenant.DefaultTenantId, (await scenario.Labels.FindByIdAsync("label-batch-null"))!.TenantId);

            var batchAssociations = new[] { Association("assoc-batch-null", "blue", "order", "order:1", tenantId: null) };
            await scenario.Associations.SaveManyAsync(batchAssociations);
            Assert.Equal(Tenant.DefaultTenantId, batchAssociations[0].TenantId);
            Assert.Equal("assoc-batch-null", Assert.Single(await scenario.AssociationQuery.FindByLabelIdsAsync(["blue"])).Id);
        }

        var agnostic = Label("label-star-2", "Star2", Tenant.AgnosticTenantId);
        await scenario.Labels.SaveAsync(agnostic);
        Assert.Equal(Tenant.AgnosticTenantId, agnostic.TenantId);

        var stampedNamedLabel = Label("label-named-stamp", "NamedStamp", tenantId: null);
        await scenario.Labels.SaveAsync(stampedNamedLabel);
        Assert.Equal("tenant-a", stampedNamedLabel.TenantId);
        Assert.Equal("tenant-a", (await scenario.Labels.FindByIdAsync("label-named-stamp"))!.TenantId);

        var stampedNamedLabelBatch = new[] { Label("label-named-batch", "NamedBatch", tenantId: null) };
        await scenario.Labels.SaveManyAsync(stampedNamedLabelBatch);
        Assert.Equal("tenant-a", stampedNamedLabelBatch[0].TenantId);
        Assert.Equal("tenant-a", (await scenario.Labels.FindByIdAsync("label-named-batch"))!.TenantId);

        var stampedNamedAssociation = Association("assoc-named-stamp", "orange", "order", "order:1", tenantId: null);
        await scenario.Associations.SaveAsync(stampedNamedAssociation);
        Assert.Equal("tenant-a", stampedNamedAssociation.TenantId);
        Assert.Equal("assoc-named-stamp", Assert.Single(await scenario.AssociationQuery.FindByLabelIdsAsync(["orange"])).Id);

        var stampedNamedAssociationBatch = new[] { Association("assoc-named-batch", "purple", "order", "order:1", tenantId: null) };
        await scenario.Associations.SaveManyAsync(stampedNamedAssociationBatch);
        Assert.Equal("tenant-a", stampedNamedAssociationBatch[0].TenantId);
        Assert.Equal("assoc-named-batch", Assert.Single(await scenario.AssociationQuery.FindByLabelIdsAsync(["purple"])).Id);
    }

    [Fact]
    public async Task NormalizedNamesAreUniquePerTenant()
    {
        await using var scenario = await CreateScenarioAsync();

        await scenario.Labels.SaveAsync(Label("label-1", "Urgent", "tenant-a"));
        await scenario.AssertUniquenessConflictAsync(() => scenario.Labels.SaveAsync(Label("label-2", "urgent", "tenant-a")));
        Assert.Equal("label-1", Assert.Single((await scenario.Labels.ListAsync()).Items).Id);

        using (scenario.UseTenant("tenant-b"))
            await scenario.Labels.SaveAsync(Label("label-b", "Urgent", "tenant-b"));
        Assert.Equal("urgent", (await scenario.Labels.FindByIdAsync("label-1"))!.NormalizedName);
        using (scenario.UseTenant("tenant-b"))
            Assert.Equal("urgent", (await scenario.Labels.FindByIdAsync("label-b"))!.NormalizedName);

        await scenario.Labels.SaveAsync(Label("label-1", "Critical", "tenant-a"));
        var updated = await scenario.Labels.FindByIdAsync("label-1");
        Assert.Equal("Critical", updated!.Name);
        Assert.Equal("critical", updated.NormalizedName);

        await scenario.Labels.SaveAsync(Label("label-later", "Later", "tenant-a"));
        await scenario.AssertUniquenessConflictAsync(() => scenario.Labels.SaveAsync(Label("label-later", "Critical", "tenant-a")));
        Assert.Equal("Later", (await scenario.Labels.FindByIdAsync("label-later"))!.Name);

        await scenario.Labels.SaveAsync(Label("label-star", "Critical", Tenant.AgnosticTenantId));
        Assert.NotNull(await scenario.Labels.FindByIdAsync("label-star"));

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            await scenario.Labels.SaveAsync(Label("label-default", "Shared", tenantId: null));
            await scenario.AssertUniquenessConflictAsync(() => scenario.Labels.SaveAsync(Label("label-default-dup", "Shared", tenantId: null)));
            Assert.Equal(Tenant.DefaultTenantId, (await scenario.Labels.FindByIdAsync("label-default"))!.TenantId);
        }

        await scenario.AssertUniquenessConflictAsync(() =>
            scenario.Labels.SaveManyAsync([Label("label-3", "Later", "tenant-a")]));
        Assert.Null(await scenario.Labels.FindByIdAsync("label-3"));

        await scenario.AssertUniquenessConflictAsync(() => scenario.Labels.SaveManyAsync(
        [
            Label("label-batch-1", "Invoice", "tenant-a"),
            Label("label-batch-2", "invoice", "tenant-a")
        ]));
        Assert.Null(await scenario.Labels.FindByIdAsync("label-batch-1"));
        Assert.Null(await scenario.Labels.FindByIdAsync("label-batch-2"));
        Assert.Equal("Later", (await scenario.Labels.FindByIdAsync("label-later"))!.Name);
    }

    private static async Task SeedMixedLabelsAsync(LabelStoreScenario scenario)
    {
        await scenario.Labels.SaveAsync(Label("label-a", "A", "tenant-a"));
        await scenario.Labels.SaveAsync(Label("label-b", "B", "tenant-b"));
        await scenario.Labels.SaveAsync(Label("label-star", "Star", Tenant.AgnosticTenantId));
    }

    private static async Task SeedMixedAssociationsAsync(LabelStoreScenario scenario)
    {
        await scenario.Associations.SaveAsync(Association("assoc-a", "red", "order", "order:1", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-b", "red", "order", "order:1", "tenant-b"));
        await scenario.Associations.SaveAsync(Association("assoc-star", "red", "order", "order:1", Tenant.AgnosticTenantId));
    }

    private static Label Label(string id, string name, string? tenantId) =>
        new()
        {
            Id = id,
            Name = name,
            TenantId = tenantId
        };

    private static WorkflowDefinitionLabel Association(
        string id,
        string labelId,
        string workflowDefinitionId,
        string workflowDefinitionVersionId,
        string? tenantId) =>
        new()
        {
            Id = id,
            LabelId = labelId,
            WorkflowDefinitionId = workflowDefinitionId,
            WorkflowDefinitionVersionId = workflowDefinitionVersionId,
            TenantId = tenantId
        };
}

[CollectionDefinition(Name)]
public sealed class LabelStoreInMemoryConformanceCollection
{
    public const string Name = "LabelStores:InMemory";
}

[CollectionDefinition(Name)]
public sealed class LabelStoreSqliteConformanceCollection
{
    public const string Name = "LabelStores:EFCore.Sqlite";
}

[Collection(LabelStoreInMemoryConformanceCollection.Name)]
public sealed class InMemoryLabelStoreConformanceTests : LabelStoreConformanceTests
{
    protected override Task<LabelStoreScenario> CreateScenarioAsync() => LabelStoreScenario.CreateInMemoryAsync();
}

[Collection(LabelStoreSqliteConformanceCollection.Name)]
public sealed class SqliteLabelStoreConformanceTests : LabelStoreConformanceTests
{
    protected override Task<LabelStoreScenario> CreateScenarioAsync() => LabelStoreScenario.CreateSqliteAsync();
}

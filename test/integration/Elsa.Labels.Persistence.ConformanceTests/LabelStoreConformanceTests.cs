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

    [Test]
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

        await Assert.That(await scenario.Labels.DeleteAsync("label-a")).IsTrue();
        await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync(["label-a"])).IsEmpty();
        await Assert.That((await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync(["label-keep"])).HasSingleItem()).Id).IsEqualTo("assoc-keep");
        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.That(await scenario.Labels.FindByIdAsync("label-b")).IsNotNull();
            await Assert.That((await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync(["label-b"])).HasSingleItem()).Id).IsEqualTo("assoc-b");
        }

        await scenario.Labels.SaveAsync(Label("label-x", "X", "tenant-a"));
        await scenario.Labels.SaveAsync(Label("label-y", "Y", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-x", "label-x", "order", "order:2", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-y", "label-y", "order", "order:2", "tenant-a"));

        await Assert.That(await scenario.Labels.DeleteManyAsync(["label-x", "label-y"])).IsEqualTo(2);
        await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync(["label-x", "label-y"])).IsEmpty();
        await Assert.That((await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync(["label-keep"])).HasSingleItem()).Id).IsEqualTo("assoc-keep");
    }

    [Test]
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
        await Assert.That(remaining).IsEquivalentTo(["assoc-blue", "assoc-green"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task AssociationFindsAndDeletesHonorWorkflowDefinitionKeys()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.Associations.SaveAsync(Association("assoc-v1-red", "red", "order", "order:1", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-v1-blue", "blue", "order", "order:1", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-v2-red", "red", "order", "order:2", "tenant-a"));
        await scenario.Associations.SaveAsync(Association("assoc-invoice", "red", "invoice", "invoice:1", "tenant-a"));

        var version1 = (await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();
        await Assert.That(version1.Count).IsEqualTo(2);
        await Assert.That(version1).Contains(x => x.Id == "assoc-v1-red");
        await Assert.That(version1).Contains(x => x.Id == "assoc-v1-blue");
        await Assert.That(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("missing")).IsEmpty();

        await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync([])).IsEmpty();
        await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync(["unknown"])).IsEmpty();
        var byRed = (await scenario.AssociationQuery.FindByLabelIdsAsync(["red"])).ToList();
        await Assert.That(byRed.Count).IsEqualTo(3);
        await Assert.That(byRed).Contains(x => x.Id == "assoc-v1-red");
        await Assert.That(byRed).Contains(x => x.Id == "assoc-v2-red");
        await Assert.That(byRed).Contains(x => x.Id == "assoc-invoice");

        var byRedAndBlue = (await scenario.AssociationQuery.FindByLabelIdsAsync(["red", "blue"])).ToList();
        await Assert.That(byRedAndBlue.Count).IsEqualTo(4);
        await Assert.That(byRedAndBlue).Contains(x => x.Id == "assoc-v1-blue");

        await Assert.That(await scenario.Associations.DeleteByWorkflowDefinitionVersionIdAsync("order:2")).IsEqualTo(1);
        await Assert.That(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:2")).IsEmpty();
        await Assert.That(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).IsNotEmpty();

        await Assert.That(await scenario.Associations.DeleteByWorkflowDefinitionIdAsync("invoice")).IsEqualTo(1);
        await Assert.That(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("invoice:1")).IsEmpty();

        await scenario.Associations.SaveManyAsync(
        [
            Association("assoc-pay-1", "red", "payroll", "payroll:1", "tenant-a"),
            Association("assoc-pay-2", "red", "payroll", "payroll:2", "tenant-a"),
            Association("assoc-hr-1", "blue", "hr", "hr:1", "tenant-a")
        ]);
        await Assert.That(await scenario.Associations.DeleteByWorkflowDefinitionVersionIdsAsync(["payroll:1", "payroll:2"])).IsEqualTo(2);
        await Assert.That(await scenario.Associations.DeleteByWorkflowDefinitionIdsAsync(["hr"])).IsEqualTo(1);
        await Assert.That(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("payroll:1")).IsEmpty();
        await Assert.That(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("hr:1")).IsEmpty();
        await Assert.That(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).IsNotEmpty();
    }

    [Test]
    public async Task ListAsyncOrdersByNameAndPages()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.Labels.SaveAsync(Label("label-z", "Zebra", "tenant-a"));
        await scenario.Labels.SaveAsync(Label("label-a", "Apple", "tenant-a"));
        await scenario.Labels.SaveAsync(Label("label-m", "Mango", "tenant-a"));

        var all = (await scenario.Labels.ListAsync()).Items.Select(x => x.Name).ToList();
        await Assert.That(all).IsEquivalentTo(["Apple", "Mango", "Zebra"], TUnit.Assertions.Enums.CollectionOrdering.Matching);

        // Page.TotalCount is not a shared contract: Memory ToPage counts after Skip/Take,
        // EF PaginateAsync counts the unpaged query.
        var firstPage = await scenario.Labels.ListAsync(PageArgs.FromRange(0, 2));
        await Assert.That(firstPage.Items.Select(x => x.Name).ToList()).IsEquivalentTo(["Apple", "Mango"], TUnit.Assertions.Enums.CollectionOrdering.Matching);

        var secondPage = await scenario.Labels.ListAsync(PageArgs.FromRange(2, 2));
        await Assert.That(secondPage.Items.Select(x => x.Name).ToList()).IsEquivalentTo(["Zebra"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task TenantStampAndIsolationHonorAmbientTenant()
    {
        await using var scenario = await CreateScenarioAsync();
        await SeedMixedLabelsAsync(scenario);
        await SeedMixedAssociationsAsync(scenario);

        var listed = (await scenario.Labels.ListAsync()).Items.ToList();
        await Assert.That(listed.Count).IsEqualTo(2);
        await Assert.That(listed).Contains(x => x.Id == "label-a");
        await Assert.That(listed).Contains(x => x.Id == "label-star");
        await Assert.That(listed).DoesNotContain(x => x.Id == "label-b");

        await Assert.That(await scenario.Labels.FindByIdAsync("label-b")).IsNull();
        await Assert.That(await scenario.Labels.FindByIdAsync("label-a")).IsNotNull();
        await Assert.That(await scenario.Labels.FindByIdAsync("label-star")).IsNotNull();

        var foundMany = (await scenario.Labels.FindManyByIdAsync(["label-a", "label-b", "label-star"])).ToList();
        await Assert.That(foundMany.Count).IsEqualTo(2);
        await Assert.That(foundMany).DoesNotContain(x => x.Id == "label-b");

        var byVersion = (await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();
        await Assert.That(byVersion.Count).IsEqualTo(2);
        await Assert.That(byVersion).Contains(x => x.Id == "assoc-a");
        await Assert.That(byVersion).Contains(x => x.Id == "assoc-star");
        await Assert.That(byVersion).DoesNotContain(x => x.Id == "assoc-b");

        var byLabel = (await scenario.AssociationQuery.FindByLabelIdsAsync(["red"])).ToList();
        await Assert.That(byLabel.Count).IsEqualTo(2);
        await Assert.That(byLabel).DoesNotContain(x => x.Id == "assoc-b");

        using (scenario.UseTenant("tenant-b"))
        {
            var tenantBLabels = (await scenario.Labels.ListAsync()).Items.ToList();
            await Assert.That(tenantBLabels).Contains(x => x.Id == "label-star");
            await Assert.That(tenantBLabels).DoesNotContain(x => x.Id == "label-a");
            await Assert.That((await scenario.Labels.FindByIdAsync("label-star"))!.Id).IsEqualTo("label-star");

            var tenantBByVersion = (await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();
            await Assert.That(tenantBByVersion.Count).IsEqualTo(2);
            await Assert.That(tenantBByVersion).Contains(x => x.Id == "assoc-b");
            await Assert.That(tenantBByVersion).Contains(x => x.Id == "assoc-star");
            await Assert.That(tenantBByVersion).DoesNotContain(x => x.Id == "assoc-a");

            var tenantBByLabel = (await scenario.AssociationQuery.FindByLabelIdsAsync(["red"])).ToList();
            await Assert.That(tenantBByLabel.Count).IsEqualTo(2);
            await Assert.That(tenantBByLabel).Contains(x => x.Id == "assoc-b");
            await Assert.That(tenantBByLabel).Contains(x => x.Id == "assoc-star");
            await Assert.That(tenantBByLabel).DoesNotContain(x => x.Id == "assoc-a");
        }

        await Assert.That(await scenario.Labels.DeleteAsync("label-b")).IsFalse();
        await Assert.That(await scenario.Associations.DeleteAsync("assoc-b")).IsFalse();
        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.That(await scenario.Labels.FindByIdAsync("label-b")).IsNotNull();
            await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync(["red"])).Contains(x => x.Id == "assoc-b");
        }

        await scenario.Associations.ReplaceAsync(
            [Association("assoc-a", "red", "order", "order:1", "tenant-a"), Association("assoc-b", "red", "order", "order:1", "tenant-b")],
            [Association("assoc-a2", "red", "order", "order:1", "tenant-a")]);
        var remainingA = (await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();
        await Assert.That(remainingA).Contains(x => x.Id == "assoc-a2");
        await Assert.That(remainingA).Contains(x => x.Id == "assoc-star");
        await Assert.That(remainingA).DoesNotContain(x => x.Id == "assoc-a");
        using (scenario.UseTenant("tenant-b"))
            await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync(["red"])).Contains(x => x.Id == "assoc-b");

        var deletedLabels = await scenario.Labels.DeleteManyAsync(["label-a", "label-b", "label-star"]);
        await Assert.That(deletedLabels).IsEqualTo(2);
        using (scenario.UseTenant("tenant-b"))
            await Assert.That((await scenario.Labels.FindByIdAsync("label-b"))!.Id).IsEqualTo("label-b");

        using (scenario.UseTenant("tenant-b"))
            await scenario.Associations.SaveAsync(Association("assoc-b-order", "blue", "order", "order:1", "tenant-b"));
        await Assert.That(await scenario.Associations.DeleteByWorkflowDefinitionIdAsync("order")).IsEqualTo(2);
        await Assert.That(await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).IsEmpty();
        using (scenario.UseTenant("tenant-b"))
        {
            var stillB = (await scenario.Associations.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();
            await Assert.That(stillB).Contains(x => x.Id == "assoc-b");
            await Assert.That(stillB).Contains(x => x.Id == "assoc-b-order");
        }

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            var stampedLabel = Label("label-null", "Null", tenantId: null);
            await scenario.Labels.SaveAsync(stampedLabel);
            await Assert.That(stampedLabel.TenantId).IsEqualTo(Tenant.DefaultTenantId);

            var namedLabel = Label("label-named", "Named", "tenant-a");
            await scenario.Labels.SaveAsync(namedLabel);
            await Assert.That(namedLabel.TenantId).IsEqualTo("tenant-a");

            var defaultLabels = (await scenario.Labels.ListAsync()).Items.ToList();
            await Assert.That(defaultLabels).Contains(x => x.Id == "label-null");
            await Assert.That(defaultLabels).DoesNotContain(x => x.Id == "label-named");

            var stampedAssociation = Association("assoc-null", "red", "order", "order:1", tenantId: null);
            await scenario.Associations.SaveAsync(stampedAssociation);
            await Assert.That(stampedAssociation.TenantId).IsEqualTo(Tenant.DefaultTenantId);
            await Assert.That((await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync(["red"])).HasSingleItem()).Id).IsEqualTo("assoc-null");

            var batchLabels = new[] { Label("label-batch-null", "BatchNull", tenantId: null) };
            await scenario.Labels.SaveManyAsync(batchLabels);
            await Assert.That(batchLabels[0].TenantId).IsEqualTo(Tenant.DefaultTenantId);
            await Assert.That((await scenario.Labels.FindByIdAsync("label-batch-null"))!.TenantId).IsEqualTo(Tenant.DefaultTenantId);

            var batchAssociations = new[] { Association("assoc-batch-null", "blue", "order", "order:1", tenantId: null) };
            await scenario.Associations.SaveManyAsync(batchAssociations);
            await Assert.That(batchAssociations[0].TenantId).IsEqualTo(Tenant.DefaultTenantId);
            await Assert.That((await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync(["blue"])).HasSingleItem()).Id).IsEqualTo("assoc-batch-null");
        }

        var agnostic = Label("label-star-2", "Star2", Tenant.AgnosticTenantId);
        await scenario.Labels.SaveAsync(agnostic);
        await Assert.That(agnostic.TenantId).IsEqualTo(Tenant.AgnosticTenantId);

        var stampedNamedLabel = Label("label-named-stamp", "NamedStamp", tenantId: null);
        await scenario.Labels.SaveAsync(stampedNamedLabel);
        await Assert.That(stampedNamedLabel.TenantId).IsEqualTo("tenant-a");
        await Assert.That((await scenario.Labels.FindByIdAsync("label-named-stamp"))!.TenantId).IsEqualTo("tenant-a");

        var stampedNamedLabelBatch = new[] { Label("label-named-batch", "NamedBatch", tenantId: null) };
        await scenario.Labels.SaveManyAsync(stampedNamedLabelBatch);
        await Assert.That(stampedNamedLabelBatch[0].TenantId).IsEqualTo("tenant-a");
        await Assert.That((await scenario.Labels.FindByIdAsync("label-named-batch"))!.TenantId).IsEqualTo("tenant-a");

        var stampedNamedAssociation = Association("assoc-named-stamp", "orange", "order", "order:1", tenantId: null);
        await scenario.Associations.SaveAsync(stampedNamedAssociation);
        await Assert.That(stampedNamedAssociation.TenantId).IsEqualTo("tenant-a");
        await Assert.That((await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync(["orange"])).HasSingleItem()).Id).IsEqualTo("assoc-named-stamp");

        var stampedNamedAssociationBatch = new[] { Association("assoc-named-batch", "purple", "order", "order:1", tenantId: null) };
        await scenario.Associations.SaveManyAsync(stampedNamedAssociationBatch);
        await Assert.That(stampedNamedAssociationBatch[0].TenantId).IsEqualTo("tenant-a");
        await Assert.That((await Assert.That(await scenario.AssociationQuery.FindByLabelIdsAsync(["purple"])).HasSingleItem()).Id).IsEqualTo("assoc-named-batch");
    }

    [Test]
    public async Task NormalizedNamesAreUniquePerTenant()
    {
        await using var scenario = await CreateScenarioAsync();

        await scenario.Labels.SaveAsync(Label("label-1", "Urgent", "tenant-a"));
        await scenario.AssertUniquenessConflictAsync(() => scenario.Labels.SaveAsync(Label("label-2", "urgent", "tenant-a")));
        await Assert.That((await Assert.That((await scenario.Labels.ListAsync()).Items).HasSingleItem()).Id).IsEqualTo("label-1");

        using (scenario.UseTenant("tenant-b"))
            await scenario.Labels.SaveAsync(Label("label-b", "Urgent", "tenant-b"));
        await Assert.That((await scenario.Labels.FindByIdAsync("label-1"))!.NormalizedName).IsEqualTo("urgent");
        using (scenario.UseTenant("tenant-b"))
            await Assert.That((await scenario.Labels.FindByIdAsync("label-b"))!.NormalizedName).IsEqualTo("urgent");

        await scenario.Labels.SaveAsync(Label("label-1", "Critical", "tenant-a"));
        var updated = await scenario.Labels.FindByIdAsync("label-1");
        await Assert.That(updated!.Name).IsEqualTo("Critical");
        await Assert.That(updated.NormalizedName).IsEqualTo("critical");

        await scenario.Labels.SaveAsync(Label("label-later", "Later", "tenant-a"));
        await scenario.AssertUniquenessConflictAsync(() => scenario.Labels.SaveAsync(Label("label-later", "Critical", "tenant-a")));
        await Assert.That((await scenario.Labels.FindByIdAsync("label-later"))!.Name).IsEqualTo("Later");

        await scenario.Labels.SaveAsync(Label("label-star", "Critical", Tenant.AgnosticTenantId));
        await Assert.That(await scenario.Labels.FindByIdAsync("label-star")).IsNotNull();

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            await scenario.Labels.SaveAsync(Label("label-default", "Shared", tenantId: null));
            await scenario.AssertUniquenessConflictAsync(() => scenario.Labels.SaveAsync(Label("label-default-dup", "Shared", tenantId: null)));
            await Assert.That((await scenario.Labels.FindByIdAsync("label-default"))!.TenantId).IsEqualTo(Tenant.DefaultTenantId);
        }

        await scenario.AssertUniquenessConflictAsync(() =>
            scenario.Labels.SaveManyAsync([Label("label-3", "Later", "tenant-a")]));
        await Assert.That(await scenario.Labels.FindByIdAsync("label-3")).IsNull();

        await scenario.AssertUniquenessConflictAsync(() => scenario.Labels.SaveManyAsync(
        [
            Label("label-batch-1", "Invoice", "tenant-a"),
            Label("label-batch-2", "invoice", "tenant-a")
        ]));
        await Assert.That(await scenario.Labels.FindByIdAsync("label-batch-1")).IsNull();
        await Assert.That(await scenario.Labels.FindByIdAsync("label-batch-2")).IsNull();
        await Assert.That((await scenario.Labels.FindByIdAsync("label-later"))!.Name).IsEqualTo("Later");
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

[InheritsTests]
public sealed class InMemoryLabelStoreConformanceTests : LabelStoreConformanceTests
{
    protected override Task<LabelStoreScenario> CreateScenarioAsync() => LabelStoreScenario.CreateInMemoryAsync();
}

[InheritsTests]
public sealed class SqliteLabelStoreConformanceTests : LabelStoreConformanceTests
{
    protected override Task<LabelStoreScenario> CreateScenarioAsync() => LabelStoreScenario.CreateSqliteAsync();
}

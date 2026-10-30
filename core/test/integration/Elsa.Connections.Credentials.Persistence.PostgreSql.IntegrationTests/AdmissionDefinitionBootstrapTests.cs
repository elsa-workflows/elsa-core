using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Elsa.Workflows.Admission.Persistence.EFCore.Features;
using Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql;
using Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.Extensions;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionDefinitionBootstrapTests(PostgreSqlConnectionsFixture fixture)
{
    [Fact]
    public async Task InsertWritesExactSharedShadowCodecAndNormalStoreReloadPreservesState()
    {
        await using var services = await CreateAsync();
        var serializer = services.GetRequiredService<IPayloadSerializer>();
        var definition = Artifact();
        definition.Options.UsableAsActivity = true;
        definition.Variables.Add(new Variable("fixture-variable", "preserved-value"));
        definition.Outcomes.Add("fixture-outcome");
        definition.CustomProperties.Add("fixture-property", "preserved-property");
        var fingerprint = AdmissionDefinitionFingerprint.Compute(definition, serializer);
        var bootstrap = services.GetRequiredService<IAdmissionDefinitionBootstrapStore>();
        await using var lease = await bootstrap.AcquireExclusiveAsync(Configuration(definition, fingerprint));
        var inserted = await bootstrap.InsertOrVerifyAsync(definition, fingerprint);
        Assert.Equal(AdmissionDefinitionInsertOutcome.Inserted, inserted);
        var reloaded = (await services.GetRequiredService<IWorkflowDefinitionStore>().FindAsync(new() { Id = definition.Id, TenantAgnostic = true }))!;
        var reloadedFingerprint = AdmissionDefinitionFingerprint.Compute(reloaded, serializer);
        Assert.Equal(fingerprint, reloadedFingerprint);
        Assert.True(reloaded.Options.UsableAsActivity);
        Assert.Single(reloaded.Variables);
        Assert.Contains("fixture-outcome", reloaded.Outcomes);
        Assert.Equal("preserved-property", reloaded.CustomProperties["fixture-property"].ToString());
        var existing = await bootstrap.InsertOrVerifyAsync(definition, fingerprint);
        Assert.Equal(AdmissionDefinitionInsertOutcome.ExistingMatch, existing);
        await using var db = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
        var count = await db.WorkflowDefinitions.CountAsync();
        Assert.Equal(1, count);
        var stored = await db.WorkflowDefinitions.SingleAsync();
        var expectedShadowState = WorkflowDefinitionStateCodec.Serialize(definition, serializer);
        var shadowState = db.Entry(stored).Property("Data").CurrentValue;
        var usableAsActivity = db.Entry(stored).Property("UsableAsActivity").CurrentValue;
        Assert.Equal(expectedShadowState, shadowState);
        Assert.Equal(true, usableAsActivity);
        await ObserveAsync("provider-bootstrap-codec-roundtrip", nameof(InsertWritesExactSharedShadowCodecAndNormalStoreReloadPreservesState),
            inserted == AdmissionDefinitionInsertOutcome.Inserted && existing == AdmissionDefinitionInsertOutcome.ExistingMatch
            && fingerprint == reloadedFingerprint && reloaded.Options.UsableAsActivity == true && reloaded.Variables.Count == 1
            && reloaded.Outcomes.Contains("fixture-outcome") && reloaded.CustomProperties["fixture-property"].ToString() == "preserved-property"
            && count == 1 && Equals(expectedShadowState, shadowState) && Equals(true, usableAsActivity),
            new() { ["insertOutcome"] = inserted.ToString(), ["existingOutcome"] = existing.ToString(),
                ["definitionCount"] = count, ["fingerprintPreserved"] = fingerprint == reloadedFingerprint,
                ["variableCount"] = reloaded.Variables.Count, ["outcomePreserved"] = reloaded.Outcomes.Contains("fixture-outcome"),
                ["customPropertyPreserved"] = reloaded.CustomProperties["fixture-property"].ToString() == "preserved-property",
                ["shadowStateMatches"] = Equals(expectedShadowState, shadowState), ["usableAsActivity"] = Equals(true, usableAsActivity) });
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("blank")]
    [InlineData("null")]
    public async Task ExistingDefaultArtifactWithMissingShadowStateFailsStrictVerification(string parameterId)
    {
        await using var services = await CreateAsync();
        var definition = Artifact();
        var fingerprint = AdmissionDefinitionFingerprint.Compute(definition, services.GetRequiredService<IPayloadSerializer>());
        var bootstrap = services.GetRequiredService<IAdmissionDefinitionBootstrapStore>();
        await using var lease = await bootstrap.AcquireExclusiveAsync(Configuration(definition, fingerprint));
        Assert.Equal(AdmissionDefinitionInsertOutcome.Inserted, await bootstrap.InsertOrVerifyAsync(definition, fingerprint));
        await using var db = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
        var invalid = parameterId switch { "missing" => null, "blank" => " ", "null" => "null", _ => throw new InvalidOperationException() };
        await db.WorkflowDefinitions.Where(x => x.Id == definition.Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => EF.Property<string?>(x, "Data"), invalid));
        var rejected = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.InsertOrVerifyAsync(definition, fingerprint));
        var stored = await db.WorkflowDefinitions.SingleAsync();
        Assert.False(stored.IsPublished);
        await ObserveAsync("provider-bootstrap-corrupt-" + parameterId, nameof(ExistingDefaultArtifactWithMissingShadowStateFailsStrictVerification),
            rejected != null && !stored.IsPublished,
            new() { ["verificationDenied"] = rejected != null, ["published"] = stored.IsPublished }, parameterId);
    }

    [Fact]
    public async Task ConflictingLogicalVersionOrTenantCannotBeAdoptedOrRetracted()
    {
        await using var services = await CreateAsync();
        var definition = Artifact();
        var store = services.GetRequiredService<IWorkflowDefinitionStore>();
        var conflicting = Artifact();
        conflicting.Id = "conflicting-version-id";
        conflicting.Version = 2;
        conflicting.IsPublished = true;
        await store.SaveAsync(conflicting);
        var serializer = services.GetRequiredService<IPayloadSerializer>();
        var fingerprint = AdmissionDefinitionFingerprint.Compute(definition, serializer);
        var bootstrap = services.GetRequiredService<IAdmissionDefinitionBootstrapStore>();
        await using var lease = await bootstrap.AcquireExclusiveAsync(Configuration(definition, fingerprint));
        var rejected = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.InsertOrVerifyAsync(definition, fingerprint));
        var unchanged = (await store.FindAsync(new() { Id = conflicting.Id, TenantAgnostic = true }))!;
        Assert.True(unchanged.IsPublished);
        var candidate = await store.FindAsync(new() { Id = definition.Id, TenantAgnostic = true });
        Assert.Null(candidate);
        await ObserveAsync("provider-bootstrap-conflicting-version", nameof(ConflictingLogicalVersionOrTenantCannotBeAdoptedOrRetracted),
            rejected != null && unchanged.IsPublished && candidate == null,
            new() { ["adoptionDenied"] = rejected != null, ["existingPublished"] = unchanged.IsPublished,
                ["candidateAbsent"] = candidate == null });
    }

    [Fact]
    public async Task WrongFullContentFingerprintOrMissingExclusiveLeaseFailsBeforeInsert()
    {
        await using var services = await CreateAsync();
        var definition = Artifact();
        var serializer = services.GetRequiredService<IPayloadSerializer>();
        var fingerprint = AdmissionDefinitionFingerprint.Compute(definition, serializer);
        var bootstrap = services.GetRequiredService<IAdmissionDefinitionBootstrapStore>();
        var missingLease = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.InsertOrVerifyAsync(definition, fingerprint));
        await using var lease = await bootstrap.AcquireExclusiveAsync(Configuration(definition, fingerprint));
        definition.Outcomes.Add("unreviewed-outcome");
        var changedFingerprint = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.InsertOrVerifyAsync(definition, fingerprint));
        await using var db = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
        var count = await db.WorkflowDefinitions.CountAsync();
        Assert.Equal(0, count);
        await ObserveAsync("provider-bootstrap-lease-fingerprint", nameof(WrongFullContentFingerprintOrMissingExclusiveLeaseFailsBeforeInsert),
            missingLease != null && changedFingerprint != null && count == 0,
            new() { ["missingLeaseDenied"] = missingLease != null, ["changedFingerprintDenied"] = changedFingerprint != null,
                ["definitionCount"] = count });
    }

    private Task ObserveAsync(string caseId, string method, bool verified, Dictionary<string, object> facts, string parameterId = "default") =>
        AdmissionProofObservation.WriteAsync(fixture, caseId, GetType().FullName + "." + method, parameterId, [],
            new Dictionary<string, bool> { ["durablePredicatesVerified"] = verified }, facts);

    private async Task<ServiceProvider> CreateAsync()
    {
        await fixture.ResetSchemaAsync();
        var provider = AdmissionWorkerHost.CreateServices(fixture.ConnectionString, includeManagement: true);
        try
        {
            await using var management = await provider.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
            await management.Database.MigrateAsync();
            await AdmissionWorkerHost.MigrateAsync(provider);
            return provider;
        }
        catch
        {
            await provider.DisposeAsync();
            throw;
        }
    }

    private static WorkflowDefinition Artifact() => AdmissionWorkerHost.Artifact();

    private static AdmissionSubscriptionConfiguration Configuration(WorkflowDefinition definition, string fingerprint) =>
        AdmissionWorkerHost.Configuration() with { DefinitionId = definition.DefinitionId, DefinitionVersionId = definition.Id, DefinitionVersion = definition.Version, DefinitionFingerprint = fingerprint };
}

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
        Assert.Equal(AdmissionDefinitionInsertOutcome.Inserted, await bootstrap.InsertOrVerifyAsync(definition, fingerprint));
        var reloaded = (await services.GetRequiredService<IWorkflowDefinitionStore>().FindAsync(new() { Id = definition.Id, TenantAgnostic = true }))!;
        Assert.Equal(fingerprint, AdmissionDefinitionFingerprint.Compute(reloaded, serializer));
        Assert.True(reloaded.Options.UsableAsActivity);
        Assert.Single(reloaded.Variables);
        Assert.Contains("fixture-outcome", reloaded.Outcomes);
        Assert.Equal("preserved-property", reloaded.CustomProperties["fixture-property"].ToString());
        Assert.Equal(AdmissionDefinitionInsertOutcome.ExistingMatch, await bootstrap.InsertOrVerifyAsync(definition, fingerprint));
        await using var db = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
        Assert.Equal(1, await db.WorkflowDefinitions.CountAsync());
        var stored = await db.WorkflowDefinitions.SingleAsync();
        Assert.Equal(WorkflowDefinitionStateCodec.Serialize(definition, serializer), db.Entry(stored).Property("Data").CurrentValue);
        Assert.Equal(true, db.Entry(stored).Property("UsableAsActivity").CurrentValue);
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
        await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.InsertOrVerifyAsync(definition, fingerprint));
        Assert.False((await db.WorkflowDefinitions.SingleAsync()).IsPublished);
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
        await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.InsertOrVerifyAsync(definition, fingerprint));
        var unchanged = (await store.FindAsync(new() { Id = conflicting.Id, TenantAgnostic = true }))!;
        Assert.True(unchanged.IsPublished);
        Assert.Null(await store.FindAsync(new() { Id = definition.Id, TenantAgnostic = true }));
    }

    [Fact]
    public async Task WrongFullContentFingerprintOrMissingExclusiveLeaseFailsBeforeInsert()
    {
        await using var services = await CreateAsync();
        var definition = Artifact();
        var serializer = services.GetRequiredService<IPayloadSerializer>();
        var fingerprint = AdmissionDefinitionFingerprint.Compute(definition, serializer);
        var bootstrap = services.GetRequiredService<IAdmissionDefinitionBootstrapStore>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.InsertOrVerifyAsync(definition, fingerprint));
        await using var lease = await bootstrap.AcquireExclusiveAsync(Configuration(definition, fingerprint));
        definition.Outcomes.Add("unreviewed-outcome");
        await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.InsertOrVerifyAsync(definition, fingerprint));
        await using var db = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
        Assert.Equal(0, await db.WorkflowDefinitions.CountAsync());
    }

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

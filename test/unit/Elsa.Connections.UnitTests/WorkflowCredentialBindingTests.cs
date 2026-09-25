using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Connections.Credentials.Persistence.EFCore.Features;
using Elsa.Connections.Credentials.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Credentials.Workflows.Features;
using Elsa.Connections.Credentials.Workflows.Services;
using Elsa.Connections.Features;
using Elsa.Connections.Models;
using Elsa.Connections.Services;
using Elsa.Extensions;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Features;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Tenants.Options;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Exceptions;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Tasks;
using Elsa.Workflows.State;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Connections.UnitTests;

public sealed class WorkflowCredentialBindingTests
{
    private const string TenantId = "tenant-a";
    private const string EnvironmentId = "integration-test";
    private const string LogicalBindingId = "payments";
    private const string LogicalBindingReferenceKey = "credentialBinding";

    [Fact]
    public async Task Resolve_UsesAmbientTenantAndConfiguredEnvironment_NotWorkflowInputs()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        await worker.SeedConnectionAsync("connection-other-tenant", "tenant-b", EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        var binding = await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a");
        Assert.True(binding.Succeeded);
        Assert.Equal(1, binding.Revision);

        var activityContext = await CreateActivityContextAsync("workflow-1", includeForgedInput: true);
        using (activityContext)
        {
            var credential = await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId);

            Assert.Equal("access-connection-a", credential.AccessToken);
            Assert.DoesNotContain("access-connection-a", JsonSerializer.Serialize(credential));
            var request = Assert.IsType<ConnectionCredentialBindingUseRequest>(worker.Authorizers.LastUseRequest);
            Assert.Equal(TenantId, request.TenantId);
            Assert.Equal(EnvironmentId, request.EnvironmentId);
            Assert.Equal("connection-a", request.ConnectionId);
            Assert.Equal(1, request.BindingRevision);
            Assert.Equal("workflow-1", request.WorkflowInstanceId);
            Assert.Equal((TenantId, EnvironmentId, "connection-a"), worker.CredentialService.LastRequest);
        }
    }

    [Fact]
    public async Task Resolve_RejectsConcurrentRebindBeforeAdmission_AndUsesNewBindingOnNextCall()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        await worker.SeedConnectionAsync("connection-b", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        Assert.True((await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);
        var activityContext = await CreateActivityContextAsync("workflow-race");
        using (activityContext)
        {
            worker.Authorizers.BeforeUseAuthorization = async request =>
            {
                Assert.Equal("connection-a", request.ConnectionId);
                var rebind = await manager.RebindAsync(Principal(), LogicalBindingId, request.BindingRevision, "connection-b");
                Assert.True(rebind.Succeeded);
                Assert.Equal(2, rebind.Revision);
            };

            await Assert.ThrowsAsync<ConnectionUnavailableException>(() => scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId));
            Assert.Equal(0, worker.CredentialService.CallCount);

            worker.Authorizers.BeforeUseAuthorization = null;
            var next = await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId);

            Assert.Equal("access-connection-b", next.AccessToken);
            Assert.Equal((TenantId, EnvironmentId, "connection-b"), worker.CredentialService.LastRequest);
            Assert.Equal(2, worker.Authorizers.LastUseRequest!.BindingRevision);

            var staleRebind = await manager.RebindAsync(Principal(), LogicalBindingId, expectedRevision: 1, "connection-a");
            Assert.False(staleRebind.Succeeded);
            Assert.Equal("connection_unavailable", staleRebind.SafeErrorCode);
        }
    }

    [Fact]
    public async Task Resolve_DoesNotFindAnotherTenantsLogicalBinding()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        using var scope = worker.Services.CreateScope();
        using (worker.TenantAccessor.PushContext(TenantContext()))
        {
            var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
            Assert.True((await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);
        }

        using (worker.TenantAccessor.PushContext(new Tenant { Id = "tenant-b", Name = "tenant-b" }))
        {
            var activityContext = await CreateActivityContextAsync("workflow-cross-tenant");
            using (activityContext)
            {
                await Assert.ThrowsAsync<ConnectionUnavailableException>(() => scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                    .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId));
                Assert.Null(worker.Authorizers.LastUseRequest);
                Assert.Equal(0, worker.CredentialService.CallCount);
            }
        }
    }

    [Fact]
    public async Task ConcurrentWorkersCreatingSameLogicalBindingReturnOneConflictWithoutOverwriting()
    {
        var insertBarrier = new BindingInsertBarrierInterceptor(workerCount: 2);
        await using var firstWorker = await Worker.CreateAsync(EnvironmentId, allow: true, saveChangesInterceptor: insertBarrier);
        await using var secondWorker = await Worker.CreateForDatabaseAsync(
            firstWorker.DatabasePath,
            EnvironmentId,
            allow: true,
            deleteDatabaseOnDispose: false,
            saveChangesInterceptor: insertBarrier);
        await firstWorker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        await firstWorker.SeedConnectionAsync("connection-b", TenantId, EnvironmentId);

        using var firstTenant = firstWorker.TenantAccessor.PushContext(TenantContext());
        using var secondTenant = secondWorker.TenantAccessor.PushContext(TenantContext());
        using var firstScope = firstWorker.Services.CreateScope();
        using var secondScope = secondWorker.Services.CreateScope();
        var firstManager = firstScope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        var secondManager = secondScope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<ConnectionCredentialBindingResult> CreateAfterStartAsync(
            IWorkflowCredentialBindingManager manager,
            string connectionId)
        {
            await start.Task;
            return await manager.CreateAsync(Principal(), LogicalBindingId, connectionId);
        }

        var firstCreate = CreateAfterStartAsync(firstManager, "connection-a");
        var secondCreate = CreateAfterStartAsync(secondManager, "connection-b");
        start.SetResult(true);
        var results = await Task.WhenAll(firstCreate, secondCreate);

        Assert.Equal(2, insertBarrier.ArrivalCount);
        Assert.Equal(1, Assert.Single(results, result => result.Succeeded).Revision);
        var rejected = Assert.Single(results, result => !result.Succeeded);
        Assert.Equal("connection_unavailable", rejected.SafeErrorCode);
        var stored = await firstScope.ServiceProvider.GetRequiredService<IConnectionCredentialBindingStore>()
            .FindAsync(TenantId, EnvironmentId, LogicalBindingId);
        Assert.NotNull(stored);
        Assert.Equal(1, stored.Revision);
        Assert.Contains(stored.ConnectionId, new[] { "connection-a", "connection-b" });

        var overflow = await firstManager.RebindAsync(Principal(), LogicalBindingId, long.MaxValue, "connection-a");
        Assert.False(overflow.Succeeded);
    }

    [Fact]
    public async Task Create_RethrowsUnrelatedDatabaseUpdateFailures()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        await using (var db = await worker.Services.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync())
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER RejectCredentialBinding BEFORE INSERT ON ConnectionCredentialBindings BEGIN SELECT RAISE(ABORT, 'synthetic database failure'); END;");
        }

        using var scope = worker.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IConnectionCredentialBindingStore>();
        await Assert.ThrowsAsync<DbUpdateException>(() => store.TryCreateAsync(
            TenantId,
            EnvironmentId,
            LogicalBindingId,
            "connection-a"));
    }

    [Fact]
    public async Task RebindAfterAuthorizationRecheck_AllowsAdmittedCallToFinishWithSnapshot()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        await worker.SeedConnectionAsync("connection-b", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        Assert.True((await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);
        var activityContext = await CreateActivityContextAsync("workflow-admitted");
        using (activityContext)
        {
            worker.CredentialService.BeforeResolve = async request =>
            {
                Assert.Equal("connection-a", request.ConnectionId);
                var rebind = await manager.RebindAsync(Principal(), LogicalBindingId, 1, "connection-b");
                Assert.True(rebind.Succeeded);
            };

            var admitted = await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId);
            Assert.Equal("access-connection-a", admitted.AccessToken);

            worker.CredentialService.BeforeResolve = null;
            var subsequent = await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId);
            Assert.Equal("access-connection-b", subsequent.AccessToken);
        }
    }

    [Fact]
    public async Task MissingEnvironmentOrDefaultAuthorizationFailsClosed()
    {
        await using var worker = await Worker.CreateAsync(environmentId: null, allow: false);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        var result = await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a");

        Assert.False(result.Succeeded);
        Assert.Equal("connection_unavailable", result.SafeErrorCode);
        Assert.Equal(0, worker.Authorizers.ManagementCallCount);

        var activityContext = await CreateActivityContextAsync("workflow-denied");
        using (activityContext)
        {
            var exception = await Assert.ThrowsAsync<ConnectionUnavailableException>(() => scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId));
            Assert.Equal("The connection is unavailable.", exception.Message);
            Assert.Equal(0, worker.CredentialService.CallCount);
        }
    }

    [Fact]
    public async Task DefaultAndAgnosticTenantContextsCannotCreateBindings()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        using var scope = worker.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        var resolver = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>();
        var activityContext = await CreateActivityContextAsync("workflow-unscoped");
        using (activityContext)
        {
            var defaultTenantResult = await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a");
            Assert.False(defaultTenantResult.Succeeded);
            await Assert.ThrowsAsync<ConnectionUnavailableException>(() => resolver.ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId));
            Assert.Equal(0, worker.Authorizers.ManagementCallCount);
            Assert.Null(worker.Authorizers.LastUseRequest);
            Assert.Equal(0, worker.CredentialService.CallCount);
        }

        using (worker.TenantAccessor.PushContext(new Tenant { Id = Tenant.AgnosticTenantId, Name = Tenant.AgnosticTenantId }))
        {
            var agnosticTenantResult = await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a");
            Assert.False(agnosticTenantResult.Succeeded);
            await Assert.ThrowsAsync<ConnectionUnavailableException>(() => resolver.ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId));
            Assert.Equal(0, worker.Authorizers.ManagementCallCount);
            Assert.Null(worker.Authorizers.LastUseRequest);
            Assert.Equal(0, worker.CredentialService.CallCount);
        }
    }

    [Fact]
    public async Task SourceEnvironmentBindingDoesNotAuthorizeTargetUntilExplicitTargetBinding()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true);
        await worker.SeedConnectionAsync("source-connection", TenantId, "source-environment");
        await worker.SeedConnectionAsync("target-connection", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IConnectionCredentialBindingStore>();
        Assert.NotNull(await store.TryCreateAsync(TenantId, "source-environment", LogicalBindingId, "source-connection"));
        var activityContext = await CreateActivityContextAsync("workflow-imported");
        using (activityContext)
        {
            var resolver = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>();
            await Assert.ThrowsAsync<ConnectionUnavailableException>(() => resolver.ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId));
            Assert.Equal(0, worker.CredentialService.CallCount);

            var targetBinding = await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>()
                .CreateAsync(Principal(), LogicalBindingId, "target-connection");
            Assert.True(targetBinding.Succeeded);
            Assert.Equal(1, targetBinding.Revision);

            var targetCredential = await resolver.ResolveAsync(activityContext.WorkflowExecutionContext, LogicalBindingId);
            Assert.Equal("access-target-connection", targetCredential.AccessToken);
            Assert.Equal((TenantId, EnvironmentId, "target-connection"), worker.CredentialService.LastRequest);
        }
    }

    [Fact]
    public async Task FeatureAuthorizersDefaultToDeny()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        module.Configure<ConnectionsFeature>();
        module.Configure<WorkflowCredentialBindingsFeature>(feature => feature.EnvironmentId = EnvironmentId);
        module.Apply();
        await using var provider = services.BuildServiceProvider();

        Assert.IsType<DenyAllConnectionCredentialBindingUseAuthorizer>(provider.GetRequiredService<IConnectionCredentialBindingUseAuthorizer>());
        Assert.IsType<DenyAllConnectionCredentialBindingManagementAuthorizer>(provider.GetRequiredService<IConnectionCredentialBindingManagementAuthorizer>());
    }

    [Fact]
    public async Task DurableGrantRequiresSeparatePolicy_AndWithdrawalStopsSubsequentUse()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true, useGrants: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var bindingManager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        Assert.True((await bindingManager.CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);

        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialGrantManager>();
        var resolver = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>();
        using var context = await CreateActivityContextAsync("granted-workflow");
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() =>
            resolver.ResolveAsync(context.WorkflowExecutionContext, LogicalBindingId));
        Assert.False((await manager.IssueAsync(Principal(), "granted-workflow", LogicalBindingId, 1)).Succeeded);
        Assert.Equal(0, worker.CredentialService.CallCount);

        worker.GrantAuthorizer.Allow = true;
        var issued = await manager.IssueAsync(Principal(), "granted-workflow", LogicalBindingId, 1);
        Assert.True(issued.Succeeded);
        Assert.Equal(1, issued.Revision);
        Assert.Equal(ConnectionCredentialGrantAction.Issue, worker.GrantAuthorizer.LastRequest?.Action);
        Assert.Equal("access-connection-a", (await resolver.ResolveAsync(
            context.WorkflowExecutionContext, LogicalBindingId)).AccessToken);

        var store = scope.ServiceProvider.GetRequiredService<IConnectionCredentialUseGrantStore>();
        var grant = await store.FindAsync(TenantId, EnvironmentId, "granted-workflow", LogicalBindingId);
        Assert.Equal("test-user", grant?.IssuedByActorId);
        Assert.Equal(1, grant?.BindingRevision);
        Assert.DoesNotContain("access-connection-a", JsonSerializer.Serialize(grant));

        var withdrawn = await manager.WithdrawAsync(Principal(), "granted-workflow", LogicalBindingId, 1);
        Assert.True(withdrawn.Succeeded);
        Assert.Equal(2, withdrawn.Revision);
        Assert.Equal(ConnectionCredentialGrantAction.Withdraw, worker.GrantAuthorizer.LastRequest?.Action);
        Assert.False((await manager.WithdrawAsync(Principal(), "granted-workflow", LogicalBindingId, 1)).Succeeded);
        Assert.False((await manager.IssueAsync(Principal(), "granted-workflow", LogicalBindingId, 1)).Succeeded);
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() =>
            resolver.ResolveAsync(context.WorkflowExecutionContext, LogicalBindingId));
        Assert.Equal(1, worker.CredentialService.CallCount);
    }

    [Fact]
    public async Task SharingRequiresItsOwnHostDecisionAndCanBeWithdrawnAfterThatDecisionIsRemoved()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true, useGrants: true,
            allowGrants: true, allowShares: false);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var bindingManager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        Assert.True((await bindingManager.CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);

        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialGrantManager>();
        var resolver = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>();
        using var context = await CreateActivityContextAsync("shared-workflow");
        Assert.False((await manager.IssueAsync(Principal(), "shared-workflow", LogicalBindingId, 1)).Succeeded);
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IConnectionCredentialUseGrantStore>()
            .FindAsync(TenantId, EnvironmentId, "shared-workflow", LogicalBindingId));
        Assert.Equal(new ConnectionCredentialShareRequest(TenantId, EnvironmentId, "shared-workflow",
            LogicalBindingId, "connection-a", 1), worker.ShareAuthorizer.LastRequest);
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() =>
            resolver.ResolveAsync(context.WorkflowExecutionContext, LogicalBindingId));

        worker.ShareAuthorizer.Allow = true;
        Assert.True((await manager.IssueAsync(Principal(), "shared-workflow", LogicalBindingId, 1)).Succeeded);
        Assert.Equal("access-connection-a", (await resolver.ResolveAsync(
            context.WorkflowExecutionContext, LogicalBindingId)).AccessToken);

        worker.ShareAuthorizer.Allow = false;
        Assert.True((await manager.WithdrawAsync(Principal(), "shared-workflow", LogicalBindingId, 1)).Succeeded);
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() =>
            resolver.ResolveAsync(context.WorkflowExecutionContext, LogicalBindingId));
        Assert.Equal(1, worker.CredentialService.CallCount);
    }

    [Fact]
    public async Task GrantFeatureWithoutHostPolicyOrPersistenceFailsClosed()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        module.Configure<ConnectionsFeature>();
        module.Configure<WorkflowCredentialBindingsFeature>(feature => feature.EnvironmentId = EnvironmentId);
        module.Configure<WorkflowCredentialUseGrantsFeature>();
        module.Apply();
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var authorizer = scope.ServiceProvider.GetRequiredService<IConnectionCredentialGrantManagementAuthorizer>();
        Assert.False(await authorizer.AuthorizeAsync(Principal(), new ConnectionCredentialGrantManagementRequest(
            TenantId, EnvironmentId, "workflow-1", LogicalBindingId, "connection-a", 1,
            ConnectionCredentialGrantAction.Issue)));
        var shareAuthorizer = scope.ServiceProvider.GetRequiredService<IConnectionCredentialShareAuthorizer>();
        Assert.False(await shareAuthorizer.AuthorizeAsync(Principal(), new ConnectionCredentialShareRequest(
            TenantId, EnvironmentId, "workflow-1", LogicalBindingId, "connection-a", 1)));
        Assert.IsType<AllowGrantControlledConnectionCredentialBindingUseAuthorizer>(
            scope.ServiceProvider.GetRequiredService<IConnectionCredentialBindingUseAuthorizer>());
        Assert.IsType<StoredConnectionCredentialBindingUseAuthorizer>(
            scope.ServiceProvider.GetRequiredService<StoredConnectionCredentialBindingUseAuthorizer>());
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IConnectionCredentialUseGrantStore>()
            .FindAsync(TenantId, EnvironmentId, "workflow-1", LogicalBindingId));
    }

    [Fact]
    public async Task GrantIsBoundToTenantEnvironmentWorkflowAndBindingRevision()
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true, useGrants: true, allowGrants: true);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        await worker.SeedConnectionAsync("connection-b", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var bindingManager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        Assert.True((await bindingManager.CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);
        var grantManager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialGrantManager>();
        var resolver = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>();
        Assert.True((await grantManager.IssueAsync(Principal(), "workflow-a", LogicalBindingId, 1)).Succeeded);
        Assert.False((await grantManager.IssueAsync(Principal(), "workflow-b", LogicalBindingId, 2)).Succeeded);
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IConnectionCredentialUseGrantStore>()
            .FindAsync(TenantId, "other-environment", "workflow-a", LogicalBindingId));
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IConnectionCredentialUseGrantStore>()
            .FindAsync("tenant-b", EnvironmentId, "workflow-a", LogicalBindingId));

        using (var wrongWorkflow = await CreateActivityContextAsync("workflow-b"))
        {
            await Assert.ThrowsAsync<ConnectionUnavailableException>(() =>
                resolver.ResolveAsync(wrongWorkflow.WorkflowExecutionContext, LogicalBindingId));
        }
        using var allowed = await CreateActivityContextAsync("workflow-a");
        Assert.Equal("access-connection-a", (await resolver.ResolveAsync(
            allowed.WorkflowExecutionContext, LogicalBindingId)).AccessToken);

        Assert.True((await bindingManager.RebindAsync(Principal(), LogicalBindingId, 1, "connection-b")).Succeeded);
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() =>
            resolver.ResolveAsync(allowed.WorkflowExecutionContext, LogicalBindingId));
        Assert.False((await grantManager.IssueAsync(Principal(), "workflow-a", LogicalBindingId, 1)).Succeeded);
        Assert.Equal(1, worker.CredentialService.CallCount);

        Assert.True((await grantManager.IssueAsync(Principal(), "workflow-c", LogicalBindingId, 2)).Succeeded);
        using var beforeDisconnect = await CreateActivityContextAsync("workflow-c");
        Assert.Equal("access-connection-b", (await resolver.ResolveAsync(
            beforeDisconnect.WorkflowExecutionContext, LogicalBindingId)).AccessToken);
        var disconnected = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>()
            .TryDisconnectAndRecordAsync("connection-b", TenantId, EnvironmentId, 1, new ConnectionOffboardingOperation
            {
                Id = "disconnect-b",
                TenantId = TenantId,
                EnvironmentId = EnvironmentId,
                ConnectionId = "connection-b",
                ProviderId = "synthetic",
                ProviderAccountId = "account-connection-b",
                Kind = ConnectionOffboardingOperationKind.LocalDisconnect,
                Status = ConnectionOffboardingOperationStatus.Completed,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        Assert.Equal(ConnectionStatus.Disconnected, disconnected?.Status);
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() =>
            resolver.ResolveAsync(beforeDisconnect.WorkflowExecutionContext, LogicalBindingId));
        Assert.Equal(2, worker.CredentialService.CallCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GrantDoesNotBypassHostUsePolicyRegardlessOfRegistrationOrder(bool registerBeforeModule)
    {
        await using var worker = await Worker.CreateAsync(EnvironmentId, allow: true, useGrants: true,
            allowGrants: true, includeHostUsePolicy: true, registerHostUsePolicyBeforeModule: registerBeforeModule);
        await worker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>()
            .CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);
        Assert.True((await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialGrantManager>()
            .IssueAsync(Principal(), "workflow-host-policy", LogicalBindingId, 1)).Succeeded);

        worker.Authorizers.UseAllowed = false;
        using var activity = await CreateActivityContextAsync("workflow-host-policy");
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() =>
            scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>()
                .ResolveAsync(activity.WorkflowExecutionContext, LogicalBindingId));
        Assert.Equal("workflow-host-policy", worker.Authorizers.LastUseRequest?.WorkflowInstanceId);
        Assert.Equal(0, worker.CredentialService.CallCount);
    }

    [Fact]
    public async Task ConcurrentWorkersCannotDuplicateOrResurrectAWithdrawnGrant()
    {
        await using var first = await Worker.CreateAsync(EnvironmentId, allow: true, useGrants: true,
            allowGrants: true, deleteDatabaseOnDispose: false);
        await first.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
        await using var second = await Worker.CreateForDatabaseAsync(first.DatabasePath, EnvironmentId,
            allow: true, deleteDatabaseOnDispose: true, useGrants: true, allowGrants: true);
        using var tenant = first.TenantAccessor.PushContext(TenantContext());
        using var scope = first.Services.CreateScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>()
            .CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);

        using var firstScope = first.Services.CreateScope();
        using var secondScope = second.Services.CreateScope();
        var firstStore = firstScope.ServiceProvider.GetRequiredService<IConnectionCredentialUseGrantStore>();
        var secondStore = secondScope.ServiceProvider.GetRequiredService<IConnectionCredentialUseGrantStore>();
        var now = DateTimeOffset.UtcNow;
        var issues = await Task.WhenAll(
            firstStore.TryIssueAsync(TenantId, EnvironmentId, "workflow-concurrent", LogicalBindingId,
                "connection-a", 1, "actor", now),
            secondStore.TryIssueAsync(TenantId, EnvironmentId, "workflow-concurrent", LogicalBindingId,
                "connection-a", 1, "actor", now));
        Assert.Single(issues, issue => issue is not null);

        var withdrawals = await Task.WhenAll(
            firstStore.TryWithdrawAsync(TenantId, EnvironmentId, "workflow-concurrent", LogicalBindingId, 1, now),
            secondStore.TryWithdrawAsync(TenantId, EnvironmentId, "workflow-concurrent", LogicalBindingId, 1, now));
        Assert.Single(withdrawals, withdrawn => withdrawn);
        var stored = await firstStore.FindAsync(TenantId, EnvironmentId, "workflow-concurrent", LogicalBindingId);
        Assert.NotNull(stored);
        Assert.False(stored.IsActive);
        Assert.Equal(2, stored.Revision);
        Assert.Null(await secondStore.TryIssueAsync(TenantId, EnvironmentId, "workflow-concurrent", LogicalBindingId,
            "connection-a", 1, "actor", now));
    }

    [Fact]
    public async Task MissingBindingStoreFailsClosedBeforeResolvingLifecycleService()
    {
        var tenantAccessor = new DefaultTenantAccessor();
        var authorizers = new TestBindingAuthorizers(allow: true);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITenantAccessor>(tenantAccessor);
        services.AddSingleton<IConnectionCredentialBindingUseAuthorizer>(authorizers);
        services.AddSingleton<IConnectionCredentialBindingManagementAuthorizer>(authorizers);

        var module = services.CreateModule();
        module.Configure<ConnectionsFeature>();
        module.Configure<WorkflowCredentialBindingsFeature>(feature => feature.EnvironmentId = EnvironmentId);
        module.Apply();

        await using var provider = services.BuildServiceProvider();
        using var tenant = tenantAccessor.PushContext(TenantContext());
        using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
        var result = await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a");

        Assert.False(result.Succeeded);
        Assert.Equal("connection_unavailable", result.SafeErrorCode);
        Assert.Equal(1, authorizers.ManagementCallCount);

        var activityContext = await CreateActivityContextAsync("workflow-no-binding-store");
        using (activityContext)
        {
            var resolver = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>();
            await Assert.ThrowsAsync<ConnectionUnavailableException>(() => resolver.ResolveAsync(
                activityContext.WorkflowExecutionContext,
                LogicalBindingId));
            Assert.Null(authorizers.LastUseRequest);
        }
    }

    [Fact]
    public async Task PersistedWorkflowRestart_RestoresTenantBeforeResolvingLogicalBinding()
    {
        const string workflowInstanceId = "persisted-workflow-1";
        var now = DateTimeOffset.Parse("2026-09-23T12:00:00Z");
        string databasePath;
        await using (var originalWorker = await Worker.CreateAsync(EnvironmentId, allow: true, deleteDatabaseOnDispose: false))
        {
            databasePath = originalWorker.DatabasePath;
            await originalWorker.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
            using var tenant = originalWorker.TenantAccessor.PushContext(TenantContext());
            using var originalScope = originalWorker.Services.CreateScope();
            var manager = originalScope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
            Assert.True((await manager.CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);

            var instances = originalScope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            await instances.SaveAsync(new WorkflowInstance
            {
                Id = workflowInstanceId,
                TenantId = TenantId,
                DefinitionId = "payments-workflow",
                DefinitionVersionId = "payments-workflow-v1",
                Version = 1,
                Status = WorkflowStatus.Running,
                SubStatus = WorkflowSubStatus.Executing,
                IsExecuting = true,
                CreatedAt = now.AddHours(-1),
                UpdatedAt = now.AddMinutes(-20),
                WorkflowState = new WorkflowState
                {
                    Id = workflowInstanceId,
                    DefinitionId = "payments-workflow",
                    DefinitionVersionId = "payments-workflow-v1",
                    DefinitionVersion = 1,
                    Status = WorkflowStatus.Running,
                    SubStatus = WorkflowSubStatus.Executing,
                    IsExecuting = true,
                    Input = new Dictionary<string, object>
                    {
                        [LogicalBindingReferenceKey] = LogicalBindingId
                    }
                }
            });
        }

        await using var worker = await Worker.CreateForDatabaseAsync(databasePath, EnvironmentId, allow: true, deleteDatabaseOnDispose: true);
        using var scope = worker.Services.CreateScope();
        var instancesStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var persistedInstance = await instancesStore.FindAsync(workflowInstanceId);
        Assert.NotNull(persistedInstance);
        var persistedJson = JsonSerializer.Serialize(persistedInstance);
        Assert.DoesNotContain("access-connection-a", persistedJson, StringComparison.Ordinal);
        Assert.DoesNotContain("generation", persistedJson, StringComparison.OrdinalIgnoreCase);
        var persistedLogicalBindingId = Assert.IsType<string>(persistedInstance.WorkflowState.Input[LogicalBindingReferenceKey]);
        Assert.Equal(LogicalBindingId, persistedLogicalBindingId);

        string? observedTenantId = null;
        string? resolvedToken = null;
        var restarter = Substitute.For<IWorkflowRestarter>();
        restarter.RestartWorkflowAsync(workflowInstanceId, Arg.Any<CancellationToken>())
            .Returns(_ => ResolveDuringRestartAsync());

        async Task ResolveDuringRestartAsync()
        {
            observedTenantId = worker.TenantAccessor.TenantId;
            var activityContext = await CreateActivityContextAsync(workflowInstanceId);
            using (activityContext)
            {
                var resolver = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialResolver>();
                resolvedToken = (await resolver.ResolveAsync(activityContext.WorkflowExecutionContext, persistedLogicalBindingId)).AccessToken;
            }
        }

        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        var tenantService = Substitute.For<ITenantService>();
        tenantService.FindAsync(TenantId, Arg.Any<CancellationToken>()).Returns(new Tenant { Id = TenantId, Name = TenantId });
        var task = new RestartInterruptedWorkflowsTask(
            restarter,
            instancesStore,
            scope.ServiceProvider.GetRequiredService<ILogger<RestartInterruptedWorkflowsTask>>(),
            Options.Create(new RuntimeOptions { InactivityThreshold = TimeSpan.FromMinutes(5), RestartInterruptedWorkflowsBatchSize = 10 }),
            clock,
            tenantService,
            worker.TenantAccessor);

        Assert.Equal(string.Empty, worker.TenantAccessor.TenantId);
        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal(TenantId, observedTenantId);
        Assert.Equal("access-connection-a", resolvedToken);
        Assert.Equal(string.Empty, worker.TenantAccessor.TenantId);
        Assert.Equal(TenantId, worker.Authorizers.LastUseRequest!.TenantId);
    }

    [Theory]
    [InlineData("allowed")]
    [InlineData("no-grant")]
    [InlineData("withdrawn")]
    [InlineData("rebound")]
    [InlineData("disconnected")]
    [InlineData("wrong-workflow")]
    [InlineData("wrong-tenant")]
    [InlineData("wrong-environment")]
    public async Task InstalledActivityResolvesDurableGrantAfterRealRuntimeResumesPersistedWorkflow(string scenario)
    {
        string databasePath;
        string workflowInstanceId;
        string bookmarkId;
        await using (var first = await Worker.CreateAsync(EnvironmentId, allow: true,
            deleteDatabaseOnDispose: false, useGrants: true, allowGrants: true, useRuntime: true))
        {
            databasePath = first.DatabasePath;
            await first.SeedConnectionAsync("connection-a", TenantId, EnvironmentId);
            if (scenario == "rebound")
            {
                await first.SeedConnectionAsync("connection-b", TenantId, EnvironmentId);
            }
            using var tenant = first.TenantAccessor.PushContext(TenantContext());
            using var scope = first.Services.CreateScope();
            var bindingManager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
            Assert.True((await bindingManager.CreateAsync(Principal(), LogicalBindingId, "connection-a")).Succeeded);

            var runtime = scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
            var client = await runtime.CreateClientAsync();
            var started = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(
                    SyntheticCredentialUseWorkflow.DefinitionId, VersionOptions.Latest)
            });
            workflowInstanceId = started.WorkflowInstanceId;
            var bookmark = Assert.Single(await scope.ServiceProvider.GetRequiredService<IBookmarkStore>()
                .FindManyAsync(new() { WorkflowInstanceId = workflowInstanceId }));
            bookmarkId = bookmark.Id;
            Assert.Empty(first.Services.GetRequiredService<SyntheticCredentialCallProbe>().Calls);
            Assert.Empty(first.Services.GetRequiredService<SyntheticCredentialCallProbe>().Attempts);

            var grantManager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialGrantManager>();
            if (scenario != "no-grant")
            {
                var grantedInstanceId = scenario == "wrong-workflow" ? "other-workflow-instance" : workflowInstanceId;
                Assert.True((await grantManager.IssueAsync(Principal(), grantedInstanceId, LogicalBindingId, 1)).Succeeded);
            }
            if (scenario == "withdrawn")
            {
                Assert.True((await grantManager.WithdrawAsync(Principal(), workflowInstanceId, LogicalBindingId, 1)).Succeeded);
            }
            if (scenario == "rebound")
            {
                Assert.True((await bindingManager.RebindAsync(Principal(), LogicalBindingId, 1, "connection-b")).Succeeded);
            }
            if (scenario == "disconnected")
            {
                var disconnected = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>()
                    .TryDisconnectAndRecordAsync("connection-a", TenantId, EnvironmentId, 1, new ConnectionOffboardingOperation
                    {
                        Id = "runtime-disconnect-a",
                        TenantId = TenantId,
                        EnvironmentId = EnvironmentId,
                        ConnectionId = "connection-a",
                        ProviderId = "synthetic",
                        ProviderAccountId = "account-connection-a",
                        Kind = ConnectionOffboardingOperationKind.LocalDisconnect,
                        Status = ConnectionOffboardingOperationStatus.Completed,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });
                Assert.Equal(ConnectionStatus.Disconnected, disconnected?.Status);
            }
            var persisted = await scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>()
                .FindAsync(workflowInstanceId);
            Assert.Equal(TenantId, persisted?.TenantId);
            Assert.DoesNotContain("access-connection-a", JsonSerializer.Serialize(persisted));
        }

        var restartedEnvironment = scenario == "wrong-environment" ? "other-environment" : EnvironmentId;
        await using var second = await Worker.CreateForDatabaseAsync(databasePath, restartedEnvironment,
            allow: true, deleteDatabaseOnDispose: true, useGrants: true, allowGrants: true, useRuntime: true);
        using var restartedTenant = second.TenantAccessor.PushContext(scenario == "wrong-tenant"
            ? new Tenant { Id = "tenant-b", Name = "tenant-b" }
            : TenantContext());
        using var restartedScope = second.Services.CreateScope();
        var restartedRuntime = restartedScope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var restartedClient = await restartedRuntime.CreateClientAsync(workflowInstanceId);
        var runError = await Record.ExceptionAsync(() => restartedClient.RunInstanceAsync(
            new RunWorkflowInstanceRequest { BookmarkId = bookmarkId }));
        var probe = second.Services.GetRequiredService<SyntheticCredentialCallProbe>();
        var calls = probe.Calls;
        var completed = await restartedScope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>()
            .FindAsync(workflowInstanceId);
        if (scenario == "wrong-tenant")
        {
            Assert.True(runError is null or WorkflowInstanceNotFoundException);
            Assert.Null(completed);
            Assert.Empty(probe.Attempts);
        }
        else
        {
            Assert.Null(runError);
            Assert.Equal(workflowInstanceId, Assert.Single(probe.Attempts));
            Assert.Equal(WorkflowStatus.Finished, completed?.Status);
        }
        if (scenario == "allowed")
        {
            var call = Assert.Single(calls);
            Assert.Equal(workflowInstanceId, call.WorkflowInstanceId);
            Assert.Equal("access-connection-a", call.AccessToken);
            Assert.Equal((TenantId, EnvironmentId, "connection-a"), second.CredentialService.LastRequest);
            Assert.Empty(completed!.WorkflowState.Incidents);
        }
        else
        {
            Assert.Empty(calls);
            Assert.Equal(0, second.CredentialService.CallCount);
            if (scenario != "wrong-tenant")
            {
                var incident = Assert.Single(completed!.WorkflowState.Incidents);
                Assert.Equal(typeof(SyntheticCredentialUseActivity).FullName, incident.ActivityType);
                Assert.Equal("The connection is unavailable.", incident.Exception?.Message);
            }
        }
        await using var database = await restartedScope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
        var connection = database.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Data FROM WorkflowInstances WHERE Id = @id";
        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "@id";
        idParameter.Value = workflowInstanceId;
        command.Parameters.Add(idParameter);
        var persistedStateJson = Assert.IsType<string>(await command.ExecuteScalarAsync());
        Assert.DoesNotContain("access-connection-a", persistedStateJson, StringComparison.Ordinal);
        if (completed != null)
        {
            var exportedState = restartedScope.ServiceProvider.GetRequiredService<IWorkflowStateSerializer>()
                .SerializeToElement(completed.WorkflowState).GetRawText();
            Assert.DoesNotContain("access-connection-a", exportedState, StringComparison.Ordinal);
            Assert.DoesNotContain("access-connection-a", JsonSerializer.Serialize(completed.WorkflowState.Output), StringComparison.Ordinal);
            Assert.DoesNotContain("access-connection-a", JsonSerializer.Serialize(completed.WorkflowState.Input), StringComparison.Ordinal);
            Assert.DoesNotContain("access-connection-a", JsonSerializer.Serialize(completed.WorkflowState.Properties), StringComparison.Ordinal);
            Assert.All(completed.WorkflowState.Incidents, incident =>
                Assert.DoesNotContain("access-connection-a", incident.Exception?.Message ?? string.Empty, StringComparison.Ordinal));
        }
        Assert.DoesNotContain(second.Logs.Messages,
            message => message.Contains("access-connection-a", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("allowed")]
    [InlineData("rotated")]
    [InlineData("disconnected")]
    [InlineData("wrong-tenant")]
    [InlineData("wrong-environment")]
    public async Task StaticApiKeyIsGrantedAndResolvedOnlyAfterPersistedWorkflowResume(string scenario)
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-workflow-api-key-{Guid.NewGuid():N}.db");
        const string originalKey = "synthetic-api-key-v1";
        const string rotatedKey = "synthetic-api-key-v2";
        string workflowInstanceId;
        string bookmarkId;
        await using (var first = await Worker.CreateForDatabaseAsync(databasePath, EnvironmentId, allow: true,
            deleteDatabaseOnDispose: false, useGrants: true, allowGrants: true, useRuntime: true, useApiKeyLifecycle: true))
        {
            using var tenant = first.TenantAccessor.PushContext(TenantContext());
            using var scope = first.Services.CreateScope();
            var apiKeys = scope.ServiceProvider.GetRequiredService<IStaticApiKeyLifecycleService>();
            var connected = await apiKeys.ConnectApiKeyAsync(Principal(), new ConnectApiKeyConnectionRequest(
                TenantId, EnvironmentId, "synthetic-api-key", "account-test", originalKey));
            Assert.True(connected.Succeeded, connected.SafeErrorCode);
            var connectionId = Assert.IsType<string>(connected.ConnectionId);
            var connectionStore = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
            var firstConnection = await connectionStore.FindAsync(connectionId, TenantId, EnvironmentId);
            var firstGeneration = Assert.IsType<string>(firstConnection?.CurrentGenerationId);
            var firstSecretName = Assert.IsType<string>(firstConnection?.CurrentSecretName);

            var bindingManager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialBindingManager>();
            Assert.True((await bindingManager.CreateAsync(Principal(), LogicalBindingId, connectionId)).Succeeded);
            var runtime = scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
            var client = await runtime.CreateClientAsync();
            var started = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(
                    SyntheticCredentialUseWorkflow.DefinitionId, VersionOptions.Latest)
            });
            workflowInstanceId = started.WorkflowInstanceId;
            bookmarkId = Assert.Single(await scope.ServiceProvider.GetRequiredService<IBookmarkStore>()
                .FindManyAsync(new() { WorkflowInstanceId = workflowInstanceId })).Id;
            Assert.Empty(first.Services.GetRequiredService<SyntheticCredentialCallProbe>().Calls);

            var grantManager = scope.ServiceProvider.GetRequiredService<IWorkflowCredentialGrantManager>();
            Assert.True((await grantManager.IssueAsync(Principal(), workflowInstanceId, LogicalBindingId, 1)).Succeeded);
            var grant = await scope.ServiceProvider.GetRequiredService<IConnectionCredentialUseGrantStore>()
                .FindAsync(TenantId, EnvironmentId, workflowInstanceId, LogicalBindingId);
            Assert.NotNull(grant);
            Assert.DoesNotContain(originalKey, JsonSerializer.Serialize(grant), StringComparison.Ordinal);
            Assert.DoesNotContain(rotatedKey, JsonSerializer.Serialize(grant), StringComparison.Ordinal);

            if (scenario == "rotated")
            {
                var rotated = await apiKeys.ReplaceApiKeyAsync(Principal(), TenantId, EnvironmentId, connectionId,
                    connected.Revision!.Value, rotatedKey);
                Assert.True(rotated.Succeeded);
                var cleanup = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                    .CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, firstGeneration);
                Assert.True(cleanup.Succeeded);
                await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.GetRequiredService<IManagedSecretManager>()
                    .ResolveGenerationAsync(firstSecretName, connectionId, firstGeneration));
            }
            else if (scenario == "disconnected")
            {
                var disconnected = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                    .DisconnectAsync(Principal(), TenantId, EnvironmentId, connectionId);
                Assert.True(disconnected.Accepted);
            }

            var persisted = await scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>().FindAsync(workflowInstanceId);
            Assert.DoesNotContain(originalKey, JsonSerializer.Serialize(persisted), StringComparison.Ordinal);
            Assert.DoesNotContain(rotatedKey, JsonSerializer.Serialize(persisted), StringComparison.Ordinal);
        }

        var restartedEnvironment = scenario == "wrong-environment" ? "other-environment" : EnvironmentId;
        await using var second = await Worker.CreateForDatabaseAsync(databasePath, restartedEnvironment, allow: true,
            deleteDatabaseOnDispose: true, useGrants: true, allowGrants: true, useRuntime: true, useApiKeyLifecycle: true);
        using var restartedTenant = second.TenantAccessor.PushContext(scenario == "wrong-tenant"
            ? new Tenant { Id = "tenant-b", Name = "tenant-b" }
            : TenantContext());
        using var restartedScope = second.Services.CreateScope();
        var restartedRuntime = restartedScope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var restartedClient = await restartedRuntime.CreateClientAsync(workflowInstanceId);
        var runError = await Record.ExceptionAsync(() => restartedClient.RunInstanceAsync(
            new RunWorkflowInstanceRequest { BookmarkId = bookmarkId }));
        var probe = second.Services.GetRequiredService<SyntheticCredentialCallProbe>();
        var completed = await restartedScope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>().FindAsync(workflowInstanceId);

        if (scenario == "wrong-tenant")
        {
            Assert.True(runError is null or WorkflowInstanceNotFoundException);
            Assert.Null(completed);
            Assert.Empty(probe.Attempts);
        }
        else
        {
            Assert.Null(runError);
            Assert.Equal(workflowInstanceId, Assert.Single(probe.Attempts));
            Assert.Equal(WorkflowStatus.Finished, completed?.Status);
        }

        if (scenario is "allowed" or "rotated")
        {
            var call = Assert.Single(probe.Calls);
            Assert.Equal(scenario == "rotated" ? rotatedKey : originalKey, call.AccessToken);
            Assert.Equal(ConnectionCredentialKind.ApiKey, call.Kind);
        }
        else
        {
            Assert.Empty(probe.Calls);
            if (scenario != "wrong-tenant")
            {
                var incident = Assert.Single(completed!.WorkflowState.Incidents);
                Assert.Equal("The connection is unavailable.", incident.Exception?.Message);
            }
        }

        await using var database = await restartedScope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
        var connection = database.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Data FROM WorkflowInstances WHERE Id = @id";
        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "@id";
        idParameter.Value = workflowInstanceId;
        command.Parameters.Add(idParameter);
        var persistedStateJson = Assert.IsType<string>(await command.ExecuteScalarAsync());
        Assert.DoesNotContain(originalKey, persistedStateJson, StringComparison.Ordinal);
        Assert.DoesNotContain(rotatedKey, persistedStateJson, StringComparison.Ordinal);
        if (completed != null)
        {
            var exportedState = restartedScope.ServiceProvider.GetRequiredService<IWorkflowStateSerializer>()
                .SerializeToElement(completed.WorkflowState).GetRawText();
            Assert.DoesNotContain(originalKey, exportedState, StringComparison.Ordinal);
            Assert.DoesNotContain(rotatedKey, exportedState, StringComparison.Ordinal);
        }
        Assert.DoesNotContain(second.Logs.Messages, message =>
            message.Contains(originalKey, StringComparison.Ordinal) || message.Contains(rotatedKey, StringComparison.Ordinal));
    }

    private static async Task<ActivityExecutionContext> CreateActivityContextAsync(string id, bool includeForgedInput = false)
    {
        var fixture = new ActivityTestFixture(new WriteLine("credential binding test"));
        if (includeForgedInput)
        {
            fixture.ConfigureContext(context =>
            {
                context.WorkflowExecutionContext.Input["tenantId"] = "tenant-b";
                context.WorkflowExecutionContext.Input["environmentId"] = "source-env";
                context.WorkflowExecutionContext.Input["connectionId"] = "connection-other-tenant";
                context.WorkflowExecutionContext.Properties["tenantId"] = "tenant-b";
            });
        }

        var activityContext = await fixture.BuildAsync();
        activityContext.WorkflowExecutionContext.Id = id;
        return activityContext;
    }

    private static ClaimsPrincipal Principal() => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test-user")], "synthetic"));

    private static Tenant TenantContext() => new() { Id = TenantId, Name = TenantId };

    private sealed class Worker(ServiceProvider services, string databasePath, DefaultTenantAccessor tenantAccessor, TestBindingAuthorizers authorizers, TestGrantAuthorizer grantAuthorizer, TestShareAuthorizer shareAuthorizer, TestCredentialService credentialService, RecordingLoggerProvider logs) : IAsyncDisposable
    {
        public ServiceProvider Services { get; } = services;
        public string DatabasePath { get; } = databasePath;
        public DefaultTenantAccessor TenantAccessor { get; } = tenantAccessor;
        public TestBindingAuthorizers Authorizers { get; } = authorizers;
        public TestGrantAuthorizer GrantAuthorizer { get; } = grantAuthorizer;
        public TestShareAuthorizer ShareAuthorizer { get; } = shareAuthorizer;
        public TestCredentialService CredentialService { get; } = credentialService;
        public RecordingLoggerProvider Logs { get; } = logs;
        private bool DeleteDatabaseOnDispose { get; init; } = true;

        public static Task<Worker> CreateAsync(
            string? environmentId,
            bool allow,
            bool deleteDatabaseOnDispose = true,
            SaveChangesInterceptor? saveChangesInterceptor = null,
            bool useGrants = false,
            bool allowGrants = false,
            bool includeHostUsePolicy = false,
            bool registerHostUsePolicyBeforeModule = false,
            bool useRuntime = false,
            bool useApiKeyLifecycle = false,
            bool allowShares = true)
        {
            var path = Path.Join(Path.GetTempPath(), $"elsa-workflow-credential-binding-{Guid.NewGuid():N}.db");
            return CreateForDatabaseAsync(path, environmentId, allow, deleteDatabaseOnDispose, saveChangesInterceptor,
                useGrants, allowGrants, includeHostUsePolicy, registerHostUsePolicyBeforeModule, useRuntime, useApiKeyLifecycle,
                allowShares);
        }

        public static async Task<Worker> CreateForDatabaseAsync(
            string path,
            string? environmentId,
            bool allow,
            bool deleteDatabaseOnDispose,
            SaveChangesInterceptor? saveChangesInterceptor = null,
            bool useGrants = false,
            bool allowGrants = false,
            bool includeHostUsePolicy = false,
            bool registerHostUsePolicyBeforeModule = false,
            bool useRuntime = false,
            bool useApiKeyLifecycle = false,
            bool allowShares = true)
        {
            var connectionString = $"Data Source={path};Cache=Shared;Pooling=False;";
            var tenantAccessor = new DefaultTenantAccessor();
            var authorizers = new TestBindingAuthorizers(allow);
            var grantAuthorizer = new TestGrantAuthorizer(allowGrants);
            var shareAuthorizer = new TestShareAuthorizer(allowShares);
            var credentialService = new TestCredentialService();
            var logs = new RecordingLoggerProvider();
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddProvider(logs));
            services.AddSingleton<ITenantAccessor>(tenantAccessor);
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.Configure<TenantsOptions>(options => options.IsEnabled = useRuntime);
            if (useApiKeyLifecycle)
            {
                services.AddSingleton<IConnectionUseAuthorizer, TestConnectionLifecycleAuthorizer>();
                services.AddSingleton<IConnectionCredentialProvider, UnusedCredentialProvider>();
            }
            if (includeHostUsePolicy && registerHostUsePolicyBeforeModule)
            {
                services.AddSingleton<IConnectionCredentialBindingUseAuthorizer>(authorizers);
            }

            var module = services.CreateModule();
            if (useApiKeyLifecycle)
            {
                var secretsFeature = module.Configure<SecretsFeature>();
                secretsFeature.ConfigureOptions = options => options.EncryptionKey = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
                secretsFeature.UseEntityFrameworkCore(feature => feature.UseSqlite(connectionString));
            }
            module.Configure<ConnectionsFeature>();
            module.Configure<EFCoreConnectionsPersistenceFeature>(feature =>
            {
                feature.UseSqlite(connectionString);
                if (saveChangesInterceptor != null)
                {
                    var configureDbContext = feature.DbContextOptionsBuilder;
                    feature.DbContextOptionsBuilder = (provider, optionsBuilder) =>
                    {
                        configureDbContext(provider, optionsBuilder);
                        optionsBuilder.AddInterceptors(saveChangesInterceptor);
                    };
                }
            });
            module.Configure<WorkflowManagementFeature>();
            module.Configure<EFCoreWorkflowInstancePersistenceFeature>(feature => feature.UseSqlite(connectionString));
            if (useRuntime)
            {
                module.Configure<EFCoreWorkflowRuntimePersistenceFeature>(feature => feature.UseSqlite(connectionString));
                module.AddWorkflow<SyntheticCredentialUseWorkflow>();
                module.AddActivity<SyntheticCredentialUseActivity>();
            }
            module.Configure<WorkflowCredentialBindingsFeature>(feature => feature.EnvironmentId = environmentId);
            if (useGrants)
            {
                module.Configure<WorkflowCredentialUseGrantsFeature>();
            }
            module.Apply();

            if (!useGrants || includeHostUsePolicy && !registerHostUsePolicyBeforeModule)
            {
                services.AddSingleton<IConnectionCredentialBindingUseAuthorizer>(authorizers);
            }
            services.AddSingleton<IConnectionCredentialBindingManagementAuthorizer>(authorizers);
            if (useGrants)
            {
                services.AddSingleton<IConnectionCredentialGrantManagementAuthorizer>(grantAuthorizer);
                services.AddSingleton<IConnectionCredentialShareAuthorizer>(shareAuthorizer);
            }
            if (!useApiKeyLifecycle)
            {
                services.AddSingleton<IConnectionBackgroundUseService>(credentialService);
            }
            if (useRuntime)
            {
                services.AddSingleton<SyntheticCredentialCallProbe>();
            }
            var serviceProvider = services.BuildServiceProvider();

            await using (var context = await serviceProvider.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync())
            {
                await context.Database.MigrateAsync();
            }
            if (useApiKeyLifecycle)
            {
                await using var context = await serviceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>().CreateDbContextAsync();
                await context.Database.MigrateAsync();
            }
            await using (var context = await serviceProvider.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                await context.Database.MigrateAsync();
            }
            if (useRuntime)
            {
                await using var context = await serviceProvider.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>().CreateDbContextAsync();
                await context.Database.MigrateAsync();
                await serviceProvider.GetRequiredService<IRegistriesPopulator>().PopulateAsync();
            }

            return new Worker(serviceProvider, path, tenantAccessor, authorizers, grantAuthorizer, shareAuthorizer,
                credentialService, logs)
            {
                DeleteDatabaseOnDispose = deleteDatabaseOnDispose
            };
        }

        public async Task SeedConnectionAsync(string connectionId, string tenantId, string environmentId)
        {
            using var scope = Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>().CreateAsync(new IntegrationConnection
            {
                Id = connectionId,
                TenantId = tenantId,
                EnvironmentId = environmentId,
                ProviderId = "synthetic",
                ProviderAccountId = $"account-{connectionId}",
                Status = ConnectionStatus.Active,
                Revision = 1,
                OperationStatus = CredentialOperationStatus.None
            });
        }

        public async ValueTask DisposeAsync()
        {
            await Services.DisposeAsync();
            if (DeleteDatabaseOnDispose && File.Exists(DatabasePath))
            {
                File.Delete(DatabasePath);
            }
        }
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider, ILogger
    {
        private readonly ConcurrentQueue<string> _messages = new();

        public IEnumerable<string> Messages => _messages;

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => this;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _messages.Enqueue($"{formatter(state, exception)} {exception}");

        public void Dispose() { }
    }

    private sealed class BindingInsertBarrierInterceptor(int workerCount) : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _allWorkersReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _remainingWorkers = workerCount;
        private int _arrivalCount;

        public int ArrivalCount => Volatile.Read(ref _arrivalCount);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var isAddingBinding = eventData.Context?.ChangeTracker.Entries<ConnectionCredentialBinding>()
                .Any(entry => entry.State == EntityState.Added) == true;
            if (!isAddingBinding)
            {
                return result;
            }

            Interlocked.Increment(ref _arrivalCount);
            if (Interlocked.Decrement(ref _remainingWorkers) == 0)
            {
                _allWorkersReady.TrySetResult();
            }

            await _allWorkersReady.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            return result;
        }
    }

    private sealed class TestBindingAuthorizers(bool allow) : IConnectionCredentialBindingUseAuthorizer, IConnectionCredentialBindingManagementAuthorizer
    {
        private readonly bool _managementAllowed = allow;
        public bool UseAllowed { get; set; } = allow;
        public int ManagementCallCount { get; private set; }
        public ConnectionCredentialBindingUseRequest? LastUseRequest { get; private set; }
        public Func<ConnectionCredentialBindingUseRequest, Task>? BeforeUseAuthorization { get; set; }

        public async Task<bool> AuthorizeAsync(ConnectionCredentialBindingUseRequest request, CancellationToken cancellationToken = default)
        {
            LastUseRequest = request;
            if (BeforeUseAuthorization != null)
            {
                await BeforeUseAuthorization(request);
            }

            return UseAllowed;
        }

        public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialBindingManagementRequest request, CancellationToken cancellationToken = default)
        {
            ManagementCallCount++;
            return Task.FromResult(_managementAllowed && principal.Identity?.IsAuthenticated == true && request.TenantId == TenantId && request.EnvironmentId == EnvironmentId);
        }
    }

    private sealed class TestGrantAuthorizer(bool allow) : IConnectionCredentialGrantManagementAuthorizer
    {
        public bool Allow { get; set; } = allow;
        public ConnectionCredentialGrantManagementRequest? LastRequest { get; private set; }

        public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialGrantManagementRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(Allow && principal.Identity?.IsAuthenticated == true &&
                request.TenantId == TenantId && request.EnvironmentId == EnvironmentId);
        }
    }

    private sealed class TestShareAuthorizer(bool allow) : IConnectionCredentialShareAuthorizer
    {
        public bool Allow { get; set; } = allow;
        public ConnectionCredentialShareRequest? LastRequest { get; private set; }

        public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialShareRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(Allow && principal.Identity?.IsAuthenticated == true &&
                request.TenantId == TenantId && request.EnvironmentId == EnvironmentId);
        }
    }

    private sealed class TestConnectionLifecycleAuthorizer : IConnectionUseAuthorizer
    {
        public Task<bool> AuthorizeAsync(ConnectionUseRequest request, CancellationToken cancellationToken = default)
        {
            if (request.TenantId != TenantId || request.EnvironmentId != EnvironmentId)
            {
                return Task.FromResult(false);
            }

            var identity = request.Principal.Identity;
            var authenticated = identity?.IsAuthenticated == true;
            var allowed = request.Kind switch
            {
                ConnectionUseKind.Human => authenticated && identity!.AuthenticationType == "synthetic",
                ConnectionUseKind.BackgroundSystem => authenticated && identity!.AuthenticationType == "Elsa.Connections.Server" &&
                    request.Principal.HasClaim("elsa:identity-kind", "system"),
                _ => false
            };
            return Task.FromResult(allowed);
        }
    }

    private sealed class UnusedCredentialProvider : IConnectionCredentialProvider
    {
        public Task<CredentialMaterial> RefreshAsync(string providerId, string accountId, string refreshToken, CancellationToken cancellationToken = default) =>
            Task.FromException<CredentialMaterial>(new InvalidOperationException("The API-key fixture must not refresh OAuth credentials."));
    }

    private sealed class TestCredentialService : IConnectionBackgroundUseService
    {
        public int CallCount { get; private set; }
        public (string TenantId, string EnvironmentId, string ConnectionId)? LastRequest { get; private set; }
        public Func<(string TenantId, string EnvironmentId, string ConnectionId), Task>? BeforeResolve { get; set; }

        public async Task<ConnectionAccessCredential> ResolveForUseAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            var request = (tenantId, environmentId, connectionId);
            LastRequest = request;
            if (BeforeResolve != null)
            {
                await BeforeResolve(request);
            }

            return new ConnectionAccessCredential($"access-{connectionId}", DateTimeOffset.UtcNow.AddHours(1));
        }
    }
}

public sealed class SyntheticCredentialUseWorkflow : WorkflowBase
{
    public const string DefinitionId = "synthetic-credential-use-workflow";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Sequence
        {
            Activities =
            {
                new Event("Resume") { Id = "Resume" },
                new SyntheticCredentialUseActivity()
            }
        };
    }
}

public sealed class SyntheticCredentialUseActivity : CodeActivity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var probe = context.GetRequiredService<SyntheticCredentialCallProbe>();
        probe.RecordAttempt(context.WorkflowExecutionContext.Id);
        var credential = await context.GetRequiredService<IWorkflowCredentialResolver>()
            .ResolveAsync(context.WorkflowExecutionContext, "payments");
        probe.Record(context.WorkflowExecutionContext.Id, credential);
    }
}

public sealed class SyntheticCredentialCallProbe
{
    public List<string> Attempts { get; } = [];
    public List<(string WorkflowInstanceId, string AccessToken, ConnectionCredentialKind Kind)> Calls { get; } = [];

    public void RecordAttempt(string workflowInstanceId) => Attempts.Add(workflowInstanceId);

    public void Record(string workflowInstanceId, ConnectionAccessCredential credential) =>
        Calls.Add((workflowInstanceId, credential.AccessToken, credential.Kind));
}

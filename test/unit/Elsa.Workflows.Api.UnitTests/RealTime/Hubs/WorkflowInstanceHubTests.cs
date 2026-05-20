using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Api.RealTime.Hubs;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using FastEndpoints;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace Elsa.Workflows.Api.UnitTests.RealTime.Hubs;

[Collection(nameof(WorkflowInstanceHubTestsCollection))]
public class WorkflowInstanceHubTests : IDisposable
{
    private const string ConnectionId = "connection-1";
    private const string WorkflowInstanceId = "workflow-instance-1";
    private const string TenantId = "tenant-a";
    private const string OtherTenantId = "tenant-b";
    private const string ReadWorkflowInstancesPermission = "read:workflow-instances";
    private const string CustomPermissionsClaimType = "elsa-permissions";
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IGroupManager _groups;
    private readonly HubCallerContext _context;
    private readonly WorkflowInstanceHub _hub;
    private WorkflowInstance? _workflowInstance;

    public WorkflowInstanceHubTests()
    {
        UsePermissionsClaimType("permissions");
        _workflowInstanceStore = Substitute.For<IWorkflowInstanceStore>();
        _tenantAccessor = Substitute.For<ITenantAccessor>();
        _groups = Substitute.For<IGroupManager>();
        _context = Substitute.For<HubCallerContext>();
        _workflowInstance = CreateWorkflowInstance(TenantId);
        _hub = new WorkflowInstanceHub(_workflowInstanceStore, _tenantAccessor)
        {
            Context = _context,
            Groups = _groups
        };

        _tenantAccessor.TenantId.Returns(TenantId);
        _context.ConnectionId.Returns(ConnectionId);
        _context.ConnectionAborted.Returns(CancellationToken.None);
        UseUser(ReadWorkflowInstancesPermission);
        StubWorkflowInstance();
    }

    public void Dispose()
    {
        UsePermissionsClaimType("permissions");
    }

    [Fact]
    public async Task ObserveInstanceAsync_WithReadPermissionAndMatchingTenant_JoinsInstanceGroup()
    {
        await _hub.ObserveInstanceAsync(WorkflowInstanceId);

        await _groups.Received(1).AddToGroupAsync(ConnectionId, WorkflowInstanceId, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("*")]
    [InlineData("read:*")]
    public async Task ObserveInstanceAsync_WithWildcardReadPermission_JoinsInstanceGroup(string permission)
    {
        UseUser(permission);

        await _hub.ObserveInstanceAsync(WorkflowInstanceId);

        await _groups.Received(1).AddToGroupAsync(ConnectionId, WorkflowInstanceId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ObserveInstanceAsync_UsesConfiguredFastEndpointsPermissionClaimType()
    {
        UsePermissionsClaimType(CustomPermissionsClaimType);
        UseUserClaim(CustomPermissionsClaimType, ReadWorkflowInstancesPermission);

        await _hub.ObserveInstanceAsync(WorkflowInstanceId);

        await _groups.Received(1).AddToGroupAsync(ConnectionId, WorkflowInstanceId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ObserveInstanceAsync_WithoutReadPermission_DoesNotJoinInstanceGroup()
    {
        UseUser("write:workflow-instances");

        await Assert.ThrowsAsync<HubException>(() => _hub.ObserveInstanceAsync(WorkflowInstanceId));

        await _groups.DidNotReceive().AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ObserveInstanceAsync_WithDifferentTenant_DoesNotJoinInstanceGroup()
    {
        UseWorkflowInstanceTenant(OtherTenantId);

        await Assert.ThrowsAsync<HubException>(() => _hub.ObserveInstanceAsync(WorkflowInstanceId));

        await _groups.DidNotReceive().AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ObserveInstanceAsync_WhenInstanceIsNotVisible_DoesNotJoinInstanceGroup()
    {
        UseWorkflowInstance(null);

        await Assert.ThrowsAsync<HubException>(() => _hub.ObserveInstanceAsync(WorkflowInstanceId));

        await _groups.DidNotReceive().AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ObserveInstanceAsync_WithTenantAgnosticInstance_JoinsInstanceGroup()
    {
        UseWorkflowInstanceTenant(Tenant.AgnosticTenantId);

        await _hub.ObserveInstanceAsync(WorkflowInstanceId);

        await _groups.Received(1).AddToGroupAsync(ConnectionId, WorkflowInstanceId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ObserveInstanceAsync_WithoutTenantAccessor_JoinsVisibleInstanceGroup()
    {
        var hub = new WorkflowInstanceHub(_workflowInstanceStore)
        {
            Context = _context,
            Groups = _groups
        };

        await hub.ObserveInstanceAsync(WorkflowInstanceId);

        await _groups.Received(1).AddToGroupAsync(ConnectionId, WorkflowInstanceId, Arg.Any<CancellationToken>());
    }

    private void UseUser(params string[] permissions)
    {
        var claims = permissions.Select(x => new Claim("permissions", x));
        var identity = new ClaimsIdentity(claims, "Test");
        _context.User.Returns(new ClaimsPrincipal(identity));
    }

    private void UseUserClaim(string claimType, params string[] permissions)
    {
        var claims = permissions.Select(x => new Claim(claimType, x));
        var identity = new ClaimsIdentity(claims, "Test");
        _context.User.Returns(new ClaimsPrincipal(identity));
    }

    private void UseWorkflowInstanceTenant(string? tenantId)
    {
        UseWorkflowInstance(CreateWorkflowInstance(tenantId));
    }

    private void UseWorkflowInstance(WorkflowInstance? workflowInstance)
    {
        _workflowInstance = workflowInstance;
        StubWorkflowInstance();
    }

    private void StubWorkflowInstance()
    {
        _workflowInstanceStore
            .FindAsync(Arg.Is<WorkflowInstanceFilter>(x => x.Id == WorkflowInstanceId), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<WorkflowInstance?>(_workflowInstance));
    }

    private static WorkflowInstance CreateWorkflowInstance(string? tenantId)
    {
        return new()
        {
            Id = WorkflowInstanceId,
            TenantId = tenantId,
            DefinitionId = "definition-1",
            DefinitionVersionId = "definition-version-1"
        };
    }

    private static void UsePermissionsClaimType(string claimType)
    {
        var property = typeof(SecurityOptions).GetProperty(nameof(SecurityOptions.PermissionsClaimType))!;
        property.SetValue(new Config().Security, claimType);
    }
}

[CollectionDefinition(nameof(WorkflowInstanceHubTestsCollection), DisableParallelization = true)]
public class WorkflowInstanceHubTestsCollection;

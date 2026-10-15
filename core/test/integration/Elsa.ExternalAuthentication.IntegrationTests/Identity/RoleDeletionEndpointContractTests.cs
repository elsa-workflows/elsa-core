using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Elsa.Authorization;
using Elsa.Identity.Contracts;
using Elsa.Identity.Features;
using Elsa.Identity.Models;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;

namespace Elsa.ExternalAuthentication.IntegrationTests.Identity;

[Collection(nameof(EndpointSecurityCollection))]
public sealed class RoleDeletionEndpointContractTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient? _client;
    private bool _wasSecurityEnabled;
    private readonly CapturingRoleDeletionCoordinator _coordinator = new();

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = false;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(IdentityFeature).Assembly];
            options.Filter = endpoint => endpoint.Namespace == "Elsa.Identity.Endpoints.Roles.Delete";
        });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IRoleDeletionCoordinator>(_coordinator);

        _app = builder.Build();
        _app.UseAuthorization();
        _app.UseFastEndpoints();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task RemediationBindsSelectedReferencesAndReplacementRole()
    {
        var response = await _client!.PostAsJsonAsync(
            "/identity/roles/target-role/remove-from-jit-policies-and-delete",
            new
            {
                expectedDependencyVersion = "dependency-version",
                confirmRemoveFromEditableJitPolicies = true,
                confirmEmptyDefaultRoles = true,
                confirmBestEffort = true,
                selectedReferences = new[] { new { source = "external-authentication", ownerId = "connection-a" } },
                replacementRoleId = "replacement-role"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(_coordinator.Command);
        Assert.Equal("target-role", _coordinator.Command.RoleId);
        Assert.Equal("dependency-version", _coordinator.Command.ExpectedDependencyVersion);
        Assert.Equal(
            new RoleDeletionReferenceSelection("external-authentication", "connection-a"),
            Assert.Single(_coordinator.Command.SelectedReferences!));
        Assert.Equal("replacement-role", _coordinator.Command.ReplacementRoleId);
    }

    private sealed class CapturingRoleDeletionCoordinator : IRoleDeletionCoordinator
    {
        public RoleDeletionRemediationCommand? Command { get; private set; }

        public ValueTask<RoleDeletionInspectionResult> InspectAsync(string roleId, ClaimsPrincipal actor, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<RoleDeletionInspectionResult>(new RoleDeletionInspectionResult.NotFound());

        public ValueTask<RoleDeletionOperationResult> DeleteAsync(string roleId, ClaimsPrincipal actor, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<RoleDeletionOperationResult>(new RoleDeletionOperationResult.NotFound());

        public ValueTask<RoleDeletionOperationResult> RemediateAndDeleteAsync(RoleDeletionRemediationCommand command, CancellationToken cancellationToken = default)
        {
            Command = command;
            var impact = new RoleDeletionImpact(
                command.RoleId,
                command.ExpectedDependencyVersion,
                RoleDeletionExecutionMode.BestEffort,
                false,
                true,
                []);
            return ValueTask.FromResult<RoleDeletionOperationResult>(new RoleDeletionOperationResult.Incomplete(impact, [], "role_dependencies_remain"));
        }
    }
}

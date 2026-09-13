using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Elsa.Authorization;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Elsa.Identity.Permissions;
using Microsoft.Extensions.DependencyInjection;
using TUnit.AspNetCore;

namespace Elsa.ExternalAuthentication.IntegrationTests.Identity;

public sealed class RoleDeletionEndpointContractTests : WebApplicationTest<RoleDeletionEndpointContractWebApplicationFactory, ExternalAuthenticationTestEntryPoint>
{
    private HttpClient? _client;
    private readonly TestAuthenticationState _authentication = new();
    private readonly CapturingRoleDeletionCoordinator _coordinator = new();

    private HttpClient Client => _client ??= Factory.CreateClient();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        _authentication.SetPermissions($"{IdentityPermissions.Roles}:{CoreVerbs.Delete}");
        services.AddSingleton(_authentication);
        services.AddSingleton<IRoleDeletionCoordinator>(_coordinator);
    }

    [Test]
    public async Task RemediationBindsSelectedReferencesAndReplacementRole()
    {
        var response = await Client.PostAsJsonAsync(
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(_coordinator.Command).IsNotNull();
        await Assert.That(_coordinator.Command.RoleId).IsEqualTo("target-role");
        await Assert.That(_coordinator.Command.ExpectedDependencyVersion).IsEqualTo("dependency-version");
        await Assert.That((await Assert.That(_coordinator.Command.SelectedReferences!).HasSingleItem())!).IsEqualTo(new RoleDeletionReferenceSelection("external-authentication", "connection-a"));
        await Assert.That(_coordinator.Command.ReplacementRoleId).IsEqualTo("replacement-role");
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

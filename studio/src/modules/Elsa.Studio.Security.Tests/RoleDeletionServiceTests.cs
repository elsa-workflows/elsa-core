using System.Net;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Refit;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class RoleDeletionServiceTests
{
    private static readonly RoleAdministrationAccess CanDelete = new(
        RoleAdministrationAccessState.Ready,
        CanView: true,
        CanCreate: false,
        CanUpdate: false,
        CanDelete: true);

    [Fact]
    public async Task InspectAsync_WhenCallerCannotDelete_FailsClosedWithoutCallingCore()
    {
        var api = new RecordingRolesApi();
        var service = CreateService(api);

        var result = await service.InspectAsync("role-1", RoleAdministrationAccess.Forbidden);

        Assert.Equal(RoleDeletionInspectionOutcome.Forbidden, result.Outcome);
        Assert.Equal(0, api.InspectCalls);
    }

    [Fact]
    public async Task InspectAsync_WhenThereAreNoReferences_ReturnsSafe()
    {
        var api = new RecordingRolesApi
        {
            Impact = new RoleDeletionImpactResponse
            {
                RoleId = "role-1",
                DependencyVersion = "dep-1",
                ExecutionMode = "atomic",
                CanDelete = true,
                CanRemediate = false
            }
        };
        var service = CreateService(api);

        var result = await service.InspectAsync("role-1", CanDelete);

        Assert.Equal(RoleDeletionInspectionOutcome.Safe, result.Outcome);
        Assert.Same(api.Impact, result.Impact);
    }

    [Fact]
    public async Task InspectAsync_WhenConfigurationReferencesExist_ReturnsBlockedWithPaths()
    {
        var impact = new RoleDeletionImpactResponse
        {
            RoleId = "role-1",
            DependencyVersion = "dep-2",
            CanDelete = false,
            ConfigurationReferences =
            [
                new RoleDeletionDependencyResponse
                {
                    Source = "external-authentication",
                    OwnerKey = "Partner SSO",
                    ConfigurationPath = "ExternalAuthentication:Policies:DefaultRoles",
                    Ownership = "configuration"
                }
            ]
        };
        var api = new RecordingRolesApi { Impact = impact };
        var service = CreateService(api);

        var result = await service.InspectAsync("role-1", CanDelete);

        Assert.Equal(RoleDeletionInspectionOutcome.Blocked, result.Outcome);
        Assert.Same(impact, result.Impact);
        Assert.Equal("ExternalAuthentication:Policies:DefaultRoles", result.Impact!.ConfigurationReferences.Single().ConfigurationPath);
    }

    [Fact]
    public async Task InspectAsync_WhenEditableReferencesExist_ReturnsRemediationRequired()
    {
        var impact = new RoleDeletionImpactResponse
        {
            RoleId = "role-1",
            DependencyVersion = "dep-3",
            ExecutionMode = "bestEffort",
            CanDelete = false,
            CanRemediate = true,
            EditableReferences =
            [
                new RoleDeletionDependencyResponse { OwnerKey = "Partner SSO", Ownership = "database" }
            ]
        };
        var api = new RecordingRolesApi { Impact = impact };
        var service = CreateService(api);

        var result = await service.InspectAsync("role-1", CanDelete);

        Assert.Equal(RoleDeletionInspectionOutcome.RemediationRequired, result.Outcome);
        Assert.Same(impact, result.Impact);
    }

    [Fact]
    public async Task RemediateAndDeleteAsync_SendsInspectedVersionAndAllExplicitConfirmations()
    {
        var api = new RecordingRolesApi();
        var service = CreateService(api);
        var confirmation = new RoleDeletionConfirmation
        {
            ExpectedDependencyVersion = "dep-4",
            ConfirmRemoveFromEditableJitPolicies = true,
            ConfirmEmptyDefaultRoles = true,
            ConfirmBestEffort = true,
            SelectedReferences = [new RoleDeletionReferenceSelection { Source = "external-authentication", OwnerId = "connection-1" }],
            ReplacementDefaultRoleId = "replacement"
        };

        var result = await service.RemediateAndDeleteAsync("role-1", CanDelete, confirmation);

        Assert.Equal(RoleDeletionOperationOutcome.Deleted, result.Outcome);
        Assert.NotNull(api.RemediationRequest);
        Assert.Equal("dep-4", api.RemediationRequest!.ExpectedDependencyVersion);
        Assert.True(api.RemediationRequest.ConfirmRemoveFromEditableJitPolicies);
        Assert.True(api.RemediationRequest.ConfirmEmptyDefaultRoles);
        Assert.True(api.RemediationRequest.ConfirmBestEffort);
        Assert.Equal([new RoleDeletionReferenceSelection { Source = "external-authentication", OwnerId = "connection-1" }], api.RemediationRequest.SelectedReferences);
        Assert.Equal("replacement", api.RemediationRequest.ReplacementRoleId);
    }

    [Fact]
    public async Task RemediateAndDeleteAsync_WhenDependencyVersionChanges_ReturnsFreshImpactWithoutRetry()
    {
        var freshImpact = new RoleDeletionImpactResponse
        {
            RoleId = "role-1",
            DependencyVersion = "dep-5",
            CanDelete = false,
            EditableReferences = [new RoleDeletionDependencyResponse { OwnerKey = "Contractor SSO" }]
        };
        var api = new RecordingRolesApi
        {
            RemediationException = CreateApiException(
                HttpStatusCode.Conflict,
                """
                {
                  "error": "conflict",
                  "message": "The role dependencies changed.",
                  "details": {
                    "code": "role_dependency_changed",
                    "deletionImpact": {
                      "roleId": "role-1",
                      "dependencyVersion": "dep-5",
                      "canDelete": false,
                      "editableReferences": [{ "ownerKey": "Contractor SSO" }]
                    }
                  }
                }
                """)
        };
        var service = CreateService(api);

        var result = await service.RemediateAndDeleteAsync(
            "role-1",
            CanDelete,
            new RoleDeletionConfirmation { ExpectedDependencyVersion = "dep-4" });

        Assert.Equal(RoleDeletionOperationOutcome.DependencyConflict, result.Outcome);
        Assert.Equal("role_dependency_changed", result.Code);
        Assert.NotNull(result.Impact);
        Assert.Equal("dep-5", result.Impact!.DependencyVersion);
        Assert.Equal(1, api.RemediationCalls);
        Assert.Equal(0, api.InspectCalls);
    }

    [Fact]
    public async Task RemediateAndDeleteAsync_WhenRemediationIsIncomplete_RetainsRoleAndReportsChangedOwners()
    {
        var api = new RecordingRolesApi
        {
            RemediationException = CreateApiException(
                HttpStatusCode.Conflict,
                """
                {
                  "error": "conflict",
                  "message": "Role-policy remediation did not complete; the role was not deleted.",
                  "details": {
                    "code": "role_remediation_incomplete",
                    "changedOwnerIds": ["partner-sso"],
                    "deletionImpact": {
                      "roleId": "role-1",
                      "dependencyVersion": "dep-6",
                      "canDelete": false,
                      "editableReferences": [{ "ownerId": "employee-sso", "ownerKey": "Employee SSO" }]
                    }
                  }
                }
                """)
        };
        var service = CreateService(api);

        var result = await service.RemediateAndDeleteAsync(
            "role-1",
            CanDelete,
            new RoleDeletionConfirmation { ExpectedDependencyVersion = "dep-5", ConfirmBestEffort = true });

        Assert.Equal(RoleDeletionOperationOutcome.Incomplete, result.Outcome);
        Assert.Equal(["partner-sso"], result.ChangedOwnerIds);
        Assert.Equal("role_remediation_incomplete", result.Code);
        Assert.Equal("Employee SSO", result.Impact!.EditableReferences.Single().OwnerKey);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, RoleDeletionOperationOutcome.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, RoleDeletionOperationOutcome.NotFound)]
    [InlineData(HttpStatusCode.BadGateway, RoleDeletionOperationOutcome.Error)]
    public async Task DeleteAsync_MapsAuthorizationNotFoundAndGeneralFailures(HttpStatusCode statusCode, RoleDeletionOperationOutcome expected)
    {
        var api = new RecordingRolesApi { DeleteException = CreateApiException(statusCode, null) };
        var service = CreateService(api);

        var result = await service.DeleteAsync("role-1", CanDelete);

        Assert.Equal(expected, result.Outcome);
    }

    private static ApiException CreateApiException(HttpStatusCode statusCode, string? content)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content is not null)
            response.Content = new StringContent(content);

        return ApiException.Create(
            new HttpRequestMessage(HttpMethod.Post, "https://elsa.example/identity/roles/role-1"),
            HttpMethod.Post,
            response,
            new RefitSettings()).GetAwaiter().GetResult();
    }

    private static RoleDeletionService CreateService(IRolesApi api) =>
        new(new StaticBackendApiClientProvider(api));

    private sealed class RecordingRolesApi : IRolesApi
    {
        public RoleDeletionImpactResponse Impact { get; init; } = new() { RoleId = "role-1", CanDelete = true };
        public ApiException? InspectException { get; init; }
        public ApiException? DeleteException { get; init; }
        public ApiException? RemediationException { get; init; }
        public RoleRemediationRequest? RemediationRequest { get; private set; }
        public int InspectCalls { get; private set; }
        public int RemediationCalls { get; private set; }

        public Task<ListRolesResponse> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CreateRoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UpdateRoleResponse> UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => DeleteException is null ? Task.CompletedTask : Task.FromException(DeleteException);

        public Task<RoleDeletionImpactResponse> GetDeletionImpactAsync(string id, CancellationToken cancellationToken = default)
        {
            InspectCalls++;
            return InspectException is null ? Task.FromResult(Impact) : Task.FromException<RoleDeletionImpactResponse>(InspectException);
        }

        public Task RemediateAndDeleteAsync(string id, RoleRemediationRequest request, CancellationToken cancellationToken = default)
        {
            RemediationCalls++;
            RemediationRequest = request;
            return RemediationException is null ? Task.CompletedTask : Task.FromException(RemediationException);
        }
    }
}

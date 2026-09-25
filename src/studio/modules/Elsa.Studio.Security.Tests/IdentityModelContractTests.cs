using System.Text.Json;
using Elsa.Studio.Security.Models;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class IdentityModelContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CreateRoleRequestHasNoIdAndSerializesTheServerGeneratedCreateShape()
    {
        var request = new CreateRoleRequest
        {
            Name = "Operators",
            Permissions = ["workflows/definitions:view"]
        };

        Assert.Null(typeof(CreateRoleRequest).GetProperty("Id"));

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, JsonOptions));
        var root = document.RootElement;

        Assert.False(root.TryGetProperty("id", out _));
        Assert.Equal("Operators", root.GetProperty("name").GetString());
        Assert.Equal("workflows/definitions:view", Assert.Single(root.GetProperty("permissions").EnumerateArray()).GetString());
    }

    [Fact]
    public void UpdateRoleRequestPreservesAnExplicitEmptyPermissionsCollection()
    {
        var request = new UpdateRoleRequest
        {
            Name = "Operators",
            Permissions = []
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, JsonOptions));
        var root = document.RootElement;

        Assert.Equal("Operators", root.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Array, root.GetProperty("permissions").ValueKind);
        Assert.Empty(root.GetProperty("permissions").EnumerateArray());
    }

    [Fact]
    public void PermissionCatalogDeserializesCoreAndDescriptorFieldsIncludingVerification()
    {
        const string payload = """
        {
          "coreVerbs": ["view", "update"],
          "resources": [{
            "resource": "workflows",
            "supportedVerbs": ["view", "update", "publish"],
            "nonCoreVerbs": ["publish"],
            "displayName": "Workflows",
            "description": "Workflow definitions and instances.",
            "category": "Authoring",
            "verified": true
          }]
        }
        """;

        var catalog = JsonSerializer.Deserialize<PermissionCatalogResponse>(payload, JsonOptions)!;
        var descriptor = Assert.Single(catalog.Resources);

        Assert.Equal(["view", "update"], catalog.CoreVerbs);
        Assert.Equal("workflows", descriptor.Resource);
        Assert.Equal(["view", "update", "publish"], descriptor.SupportedVerbs);
        Assert.Equal(["publish"], descriptor.NonCoreVerbs);
        Assert.Equal("Workflows", descriptor.DisplayName);
        Assert.Equal("Workflow definitions and instances.", descriptor.Description);
        Assert.Equal("Authoring", descriptor.Category);
        Assert.True(descriptor.Verified);
    }

    [Fact]
    public void PermissionReachDeserializesWildcardCoverageAndCount()
    {
        var reach = JsonSerializer.Deserialize<PermissionReachResponse>(
            """{ "resource": "workflows/*", "covers": ["workflows/definitions", "workflows/instances"], "count": 2 }""", JsonOptions)!;

        Assert.Equal("workflows/*", reach.Resource);
        Assert.Equal(["workflows/definitions", "workflows/instances"], reach.Covers);
        Assert.Equal(2, reach.Count);
    }

    [Fact]
    public void DeletionImpactDeserializesVersionedReferencesAndRemediationFlags()
    {
        const string payload = """
        {
          "roleId": "role-1",
          "dependencyVersion": "dep-7",
          "executionMode": "bestEffort",
          "canDelete": false,
          "canRemediate": true,
          "configurationReferences": [{
            "source": "workflow-definition",
            "ownerId": "definition-1",
            "ownerKey": "invoice",
            "policyBranch": "default",
            "ownership": "configuration",
            "configurationPath": "roles[0]",
            "revision": 11,
            "removesLastDefaultRole": false
          }],
          "editableReferences": [],
          "warnings": ["A review is required."]
        }
        """;

        var impact = JsonSerializer.Deserialize<RoleDeletionImpactResponse>(payload, JsonOptions)!;
        var dependency = Assert.Single(impact.ConfigurationReferences);

        Assert.Equal("role-1", impact.RoleId);
        Assert.Equal("dep-7", impact.DependencyVersion);
        Assert.Equal("bestEffort", impact.ExecutionMode);
        Assert.False(impact.CanDelete);
        Assert.True(impact.CanRemediate);
        Assert.Equal("workflow-definition", dependency.Source);
        Assert.Equal("definition-1", dependency.OwnerId);
        Assert.Equal("invoice", dependency.OwnerKey);
        Assert.Equal("default", dependency.PolicyBranch);
        Assert.Equal("configuration", dependency.Ownership);
        Assert.Equal("roles[0]", dependency.ConfigurationPath);
        Assert.Equal(11, dependency.Revision);
        Assert.False(dependency.RemovesLastDefaultRole);
        Assert.Equal("A review is required.", Assert.Single(impact.Warnings));
    }

    [Fact]
    public void RemediationAndErrorModelsPreserveConfirmationAndStructuredFailureFields()
    {
        var request = new RoleRemediationRequest
        {
            ExpectedDependencyVersion = "dep-7",
            ConfirmRemoveFromEditableJitPolicies = true,
            ConfirmEmptyDefaultRoles = false,
            ConfirmBestEffort = true,
            SelectedReferences = [new RoleDeletionReferenceSelection { Source = "external-authentication", OwnerId = "connection-1" }],
            ReplacementRoleId = "workflow-authors"
        };
        using var requestDocument = JsonDocument.Parse(JsonSerializer.Serialize(request, JsonOptions));

        Assert.Equal("dep-7", requestDocument.RootElement.GetProperty("expectedDependencyVersion").GetString());
        Assert.True(requestDocument.RootElement.GetProperty("confirmRemoveFromEditableJitPolicies").GetBoolean());
        Assert.False(requestDocument.RootElement.GetProperty("confirmEmptyDefaultRoles").GetBoolean());
        Assert.True(requestDocument.RootElement.GetProperty("confirmBestEffort").GetBoolean());
        var selection = Assert.Single(requestDocument.RootElement.GetProperty("selectedReferences").EnumerateArray());
        Assert.Equal("external-authentication", selection.GetProperty("source").GetString());
        Assert.Equal("connection-1", selection.GetProperty("ownerId").GetString());
        Assert.Equal("workflow-authors", requestDocument.RootElement.GetProperty("replacementRoleId").GetString());

        var coreError = JsonSerializer.Deserialize<CoreApiErrorResponse>(
            """{ "error": "role_referenced_by_jit_policy", "message": "The role is still referenced.", "details": { "roleId": "role-1" } }""", JsonOptions)!;
        Assert.Equal("role_referenced_by_jit_policy", coreError.Error);
        Assert.Equal("The role is still referenced.", coreError.Message);
        Assert.Equal("role-1", coreError.Details!.Value.GetProperty("roleId").GetString());

        var validationError = JsonSerializer.Deserialize<ValidationApiErrorResponse>(
            """{ "statusCode": 400, "message": "One or more validation errors occurred.", "errors": { "Name": ["The name is required."] } }""", JsonOptions)!;
        Assert.Equal(400, validationError.StatusCode);
        Assert.Equal("One or more validation errors occurred.", validationError.Message);
        Assert.Equal("The name is required.", Assert.Single(validationError.Errors["Name"]));
    }

    [Fact]
    public void CreateUserResponseCarriesOnlyTheGeneratedPasswordAndNoCredentialMaterial()
    {
        var response = JsonSerializer.Deserialize<CreateUserResponse>(
            """{ "id": "user-1", "name": "alice", "roles": ["admin"], "tenantId": "tenant-a", "generatedPassword": "once-only" }""", JsonOptions)!;

        Assert.Equal("user-1", response.Id);
        Assert.Equal("alice", response.Name);
        Assert.Equal(["admin"], response.Roles);
        Assert.Equal("tenant-a", response.TenantId);
        Assert.Equal("once-only", response.GeneratedPassword);

        var withoutGeneratedPassword = JsonSerializer.Deserialize<CreateUserResponse>("""{ "id": "user-1", "name": "alice" }""", JsonOptions)!;
        Assert.Null(withoutGeneratedPassword.GeneratedPassword);

        var propertyNames = typeof(CreateUserResponse).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("Password", propertyNames);
        Assert.DoesNotContain(propertyNames, x => x.Contains("Hash", StringComparison.OrdinalIgnoreCase) || x.Contains("Salt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LegacyClaimBasedUserPermissionsNoLongerExist()
    {
        var assembly = typeof(UserSummary).Assembly;

        Assert.Null(assembly.GetType("Elsa.Studio.Security.Models.IdentityClaimPermissions"));
        Assert.Null(assembly.GetType("Elsa.Studio.Security.Services.IIdentityPermissionService"));
        Assert.Equal("identity/users", Elsa.Studio.Security.Constants.IdentityPermissions.UsersResource);
    }
}

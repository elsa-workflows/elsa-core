using System.Security.Claims;
using Elsa.Authorization;
using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Endpoints.Applications.Create;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.UnitTests.Shared;
using Elsa.Workflows;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Endpoints;

[Collection(nameof(FastEndpointsCollection))]
public class CreateApplicationRoleBoundaryTests
{
    private const string CreateApplicationPermission = "identity/applications:create";
    private const string ReadDefinitionsPermission = "workflows/definitions:view";

    private readonly IIdentityGenerator _identityGenerator = Substitute.For<IIdentityGenerator>();
    private readonly IClientIdGenerator _clientIdGenerator = Substitute.For<IClientIdGenerator>();
    private readonly ISecretGenerator _secretGenerator = Substitute.For<ISecretGenerator>();
    private readonly IApiKeyGenerator _apiKeyGenerator = Substitute.For<IApiKeyGenerator>();
    private readonly ISecretHasher _secretHasher = Substitute.For<ISecretHasher>();
    private readonly IApplicationStore _applicationStore = Substitute.For<IApplicationStore>();
    private readonly MemoryStore<Role> _roles = new();

    public CreateApplicationRoleBoundaryTests()
    {
        _roles.Save(new Role { Id = "reader", Name = "Reader", TenantId = "tenant-a", Permissions = [ReadDefinitionsPermission] }, x => x.Id);
        _roles.Save(new Role { Id = "other-tenant-reader", Name = "Reader", TenantId = "tenant-b", Permissions = [ReadDefinitionsPermission] }, x => x.Id);
        _roles.Save(new Role { Id = "admin", Name = "Admin", TenantId = "tenant-a", Permissions = [PermissionNames.All] }, x => x.Id);

        _identityGenerator.GenerateId().Returns("app-1");
        _clientIdGenerator.GenerateAsync(Arg.Any<CancellationToken>()).Returns("client-1");
        _secretGenerator.Generate().Returns("generated-secret");
        _apiKeyGenerator.Generate("client-1").Returns("generated-key");
        _secretHasher.HashSecret(Arg.Any<string>()).Returns(new HashedSecret([1], [2]));
    }

    [Theory]
    [InlineData("admin", ReadDefinitionsPermission)]
    [InlineData("unknown-role", PermissionNames.All)]
    [InlineData("other-tenant-reader", PermissionNames.All)]
    public async Task ForbiddenRolesAreRejectedBeforeGeneratingCredentialsOrSaving(string roleId, string callerPermission)
    {
        var endpoint = CreateEndpoint(callerPermission);

        await endpoint.HandleAsync(new Request { Name = "Application", Roles = [roleId] }, CancellationToken.None);

        Assert.Equal(StatusCodes.Status403Forbidden, endpoint.HttpContext.Response.StatusCode);
        _identityGenerator.DidNotReceiveWithAnyArgs().GenerateId();
        await _clientIdGenerator.DidNotReceiveWithAnyArgs().GenerateAsync();
        _secretGenerator.DidNotReceiveWithAnyArgs().Generate();
        _apiKeyGenerator.DidNotReceiveWithAnyArgs().Generate(default!);
        _secretHasher.DidNotReceiveWithAnyArgs().HashSecret(default!);
        await _applicationStore.DidNotReceiveWithAnyArgs().SaveAsync(default!);
    }

    [Theory]
    [InlineData("reader", ReadDefinitionsPermission)]
    [InlineData(null, null)]
    public async Task ApplicationCreationKeepsAllowedRolesAndReturnsCredentials(string? roleId, string? callerPermission)
    {
        var endpoint = callerPermission == null ? CreateEndpoint() : CreateEndpoint(callerPermission);
        string[]? roles = roleId == null ? null : [roleId];

        await endpoint.HandleAsync(new Request { Name = "Application", Roles = roles }, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(roles ?? [], endpoint.Response.Roles);
        Assert.Equal("app-1", endpoint.Response.Id);
        Assert.Equal("Application", endpoint.Response.Name);
        Assert.Equal("client-1", endpoint.Response.ClientId);
        Assert.Equal("generated-secret", endpoint.Response.ClientSecret);
        Assert.Equal("generated-key", endpoint.Response.ApiKey);
        await _applicationStore.Received(1).SaveAsync(
            Arg.Is<Application>(x => x.Id == "app-1" && x.Roles.SequenceEqual(roles ?? Array.Empty<string>())),
            Arg.Any<CancellationToken>());
    }

    private Create CreateEndpoint(params string[] callerPermissions)
    {
        var roleStore = new MemoryRoleStore(_roles, new TestTenantAccessor("tenant-a"));
        var roleAuthorization = new RoleAuthorizationService(new StoreBasedRoleProvider(roleStore), new PermissionEvaluator());
        var claims = callerPermissions.Prepend(CreateApplicationPermission).Select(x => new Claim(PermissionNames.ClaimType, x));
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        return Factory.Create<Create>(
            context => context.User = user,
            _identityGenerator,
            _clientIdGenerator,
            _secretGenerator,
            _apiKeyGenerator,
            _secretHasher,
            _applicationStore,
            roleAuthorization);
    }
}

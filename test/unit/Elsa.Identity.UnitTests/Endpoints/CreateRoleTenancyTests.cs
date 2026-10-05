using System.Security.Claims;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Endpoints.Roles.Create;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Mediator.Contracts;
using Elsa.Permissions;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.UnitTests.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Exceptions;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Endpoints;

/// <summary>
/// <c>POST /identity/roles</c> against one store shared by two tenants: a role name is unique per tenant, not per
/// store (#8615).
/// </summary>
[Collection(nameof(FastEndpointsCollection))]
public class CreateRoleTenancyTests
{
    private static readonly Tenant TenantB = new() { Id = "tenant-b", Name = "Tenant B" };
    private readonly TestTenantAccessor _tenantAccessor = new("tenant-a");
    private readonly MemoryRoleStore _roleStore;

    public CreateRoleTenancyTests() => _roleStore = new MemoryRoleStore(new MemoryStore<Role>(), _tenantAccessor);

    [Fact]
    public async Task TwoTenantsCanEachCreateASameNamedRoleWithoutAnId()
    {
        var created = await CreateAsync("Operators");
        Assert.Equal(StatusCodes.Status200OK, created.HttpContext.Response.StatusCode);

        Create createdInB;
        using (_tenantAccessor.PushContext(TenantB))
        {
            createdInB = await CreateAsync("Operators");
            Assert.Equal(StatusCodes.Status200OK, createdInB.HttpContext.Response.StatusCode);
            Assert.Equal(createdInB.Response.Id, (await _roleStore.FindByNameAsync("Operators"))?.Id);
        }

        Assert.NotEqual(created.Response.Id, createdInB.Response.Id);
        Assert.NotEqual("operators", created.Response.Id);
        Assert.Equal(created.Response.Id, (await _roleStore.FindByNameAsync("Operators"))?.Id);
    }

    [Fact]
    public async Task ASameNamedRoleInTheSameTenantIsAConflict()
    {
        await CreateAsync("Operators");

        var duplicate = await CreateAsync("Operators");

        Assert.Equal(StatusCodes.Status409Conflict, duplicate.HttpContext.Response.StatusCode);
        Assert.Single(await _roleStore.FindManyAsync(new()));
    }

    [Fact]
    public async Task AUniqueIndexViolationFromAConcurrentCreateIsAConflict()
    {
        var roleManager = Substitute.For<IRoleManager>();
        roleManager.CreateRoleAsync(Arg.Any<string>(), Arg.Any<ICollection<string>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<CreateRoleResult>(_ => throw new UniqueKeyConstraintViolationException("Unable to save data", new Exception()));

        var endpoint = await CreateAsync("Operators", roleManager);

        Assert.Equal(StatusCodes.Status409Conflict, endpoint.HttpContext.Response.StatusCode);
    }

    private async Task<Create> CreateAsync(string name, IRoleManager? roleManager = null)
    {
        var roleAuthorization = Substitute.For<IRoleAuthorizationService>();
        roleAuthorization.CanCreateRoleWithPermissions(Arg.Any<ClaimsPrincipal>(), Arg.Any<IEnumerable<string>?>()).Returns(true);
        var grantValidator = Substitute.For<IPermissionGrantValidator>();
        grantValidator.Validate(Arg.Any<IEnumerable<string>?>()).Returns(PermissionGrantValidationResult.Valid);
        roleManager ??= new RoleManager(_roleStore, new StoreBasedRoleProvider(_roleStore), _tenantAccessor, new GuidIdentityGenerator());
        var notifier = new RoleSecurityNotifier(Substitute.For<INotificationSender>(), _tenantAccessor, Substitute.For<ISystemClock>());
        var endpoint = Factory.Create<Create>(context => context.Response.Body = new MemoryStream(), roleManager, roleAuthorization, grantValidator, notifier);

        await endpoint.HandleAsync(new Request { Name = name, Permissions = ["workflows/*:view"] }, CancellationToken.None);
        return endpoint;
    }
}

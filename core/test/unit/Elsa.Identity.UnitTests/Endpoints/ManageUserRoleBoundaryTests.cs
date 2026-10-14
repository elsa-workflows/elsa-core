using System.Security.Claims;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.UnitTests.Shared;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using DeleteEndpoint = Elsa.Identity.Endpoints.Users.Delete.Delete;
using UpdateEndpoint = Elsa.Identity.Endpoints.Users.Update.Update;
using UpdateRequest = Elsa.Identity.Endpoints.Users.Update.Request;

namespace Elsa.Identity.UnitTests.Endpoints;

/// <summary>
/// Pins that <c>PUT</c> and <c>DELETE /identity/users/{id}</c> only act on users whose current roles the caller could assign.
/// </summary>
[Collection(nameof(FastEndpointsCollection))]
public class ManageUserRoleBoundaryTests
{
    private const string UserId = "admin-user";
    private static readonly string[] AdminRoles = ["admin"];

    private readonly IUserStore _userStore = Substitute.For<IUserStore>();
    private readonly ISecretHasher _secretHasher = Substitute.For<ISecretHasher>();
    private readonly IUserDeletionCoordinator _deletionCoordinator = Substitute.For<IUserDeletionCoordinator>();
    private readonly IRoleAuthorizationService _roleAuthorization = Substitute.For<IRoleAuthorizationService>();
    private readonly User _user = new() { Id = UserId, Name = "admin", Roles = AdminRoles, HashedPassword = "original-hash", HashedPasswordSalt = "original-salt" };

    public ManageUserRoleBoundaryTests()
    {
        _userStore.FindAsync(Arg.Is<UserFilter>(x => x.Id == UserId), Arg.Any<CancellationToken>()).Returns(_user);
        _deletionCoordinator.DeleteAsync(UserId, Arg.Any<CancellationToken>()).Returns(new UserDeletionOperationResult.Deleted());
    }

    [Fact]
    public async Task UpdateIsForbiddenWhenCallerCannotAssignTheUsersCurrentRoles()
    {
        CallerCanAssignCurrentRoles(false);
        var endpoint = CreateUpdateEndpoint();

        await endpoint.HandleAsync(new UpdateRequest { Password = "new-password" }, CancellationToken.None);

        Assert.Equal(StatusCodes.Status403Forbidden, endpoint.HttpContext.Response.StatusCode);
        Assert.Equal("original-hash", _user.HashedPassword);
        _secretHasher.DidNotReceiveWithAnyArgs().HashSecret(default!);
        await _userStore.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
    }

    [Fact]
    public async Task UpdateChangesPasswordWhenCallerCanAssignTheUsersCurrentRoles()
    {
        CallerCanAssignCurrentRoles(true);
        _secretHasher.HashSecret("new-password").Returns(new HashedSecret([1], [2]));
        var endpoint = CreateUpdateEndpoint();

        await endpoint.HandleAsync(new UpdateRequest { Password = "new-password" }, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, endpoint.HttpContext.Response.StatusCode);
        Assert.NotEqual("original-hash", _user.HashedPassword);
        await _userStore.Received(1).SaveAsync(_user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteIsForbiddenWhenCallerCannotAssignTheUsersCurrentRoles()
    {
        CallerCanAssignCurrentRoles(false);
        var endpoint = CreateDeleteEndpoint();

        await endpoint.HandleAsync(CancellationToken.None);

        Assert.Equal(StatusCodes.Status403Forbidden, endpoint.HttpContext.Response.StatusCode);
        await _deletionCoordinator.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    [Fact]
    public async Task DeleteSucceedsWhenCallerCanAssignTheUsersCurrentRoles()
    {
        CallerCanAssignCurrentRoles(true);
        var endpoint = CreateDeleteEndpoint();

        await endpoint.HandleAsync(CancellationToken.None);

        Assert.Equal(StatusCodes.Status204NoContent, endpoint.HttpContext.Response.StatusCode);
        await _deletionCoordinator.Received(1).DeleteAsync(UserId, Arg.Any<CancellationToken>());
    }

    private void CallerCanAssignCurrentRoles(bool allowed) =>
        _roleAuthorization.CanAssignRolesAsync(Arg.Any<ClaimsPrincipal>(), Arg.Is<IEnumerable<string>?>(x => x != null && x.SequenceEqual(AdminRoles)), Arg.Any<CancellationToken>()).Returns(allowed);

    private UpdateEndpoint CreateUpdateEndpoint() =>
        Factory.Create<UpdateEndpoint>(WithUserIdRoute, _userStore, _secretHasher, _roleAuthorization);

    private DeleteEndpoint CreateDeleteEndpoint() =>
        Factory.Create<DeleteEndpoint>(WithUserIdRoute, _deletionCoordinator, _userStore, _roleAuthorization);

    private static void WithUserIdRoute(DefaultHttpContext context) => context.Request.RouteValues["id"] = UserId;
}

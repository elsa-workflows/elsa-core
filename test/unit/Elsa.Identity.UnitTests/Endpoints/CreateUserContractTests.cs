using System.Reflection;
using Elsa.Identity.Contracts;
using Elsa.Identity.Endpoints.Users.Create;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Endpoints;

/// <summary>
/// Pins the <c>POST /identity/users</c> response contract: no hashes or salts, no echo of a supplied password,
/// and a generated password returned exactly once.
/// </summary>
public class CreateUserContractTests
{
    private static readonly User StoredUser = new()
    {
        Id = "user-1",
        Name = "alice",
        Roles = ["admin"],
        TenantId = "tenant-a",
        HashedPassword = "hash-must-not-leak",
        HashedPasswordSalt = "salt-must-not-leak"
    };

    [Fact]
    public void ResponseExposesOnlyAccountFieldsAndTheGeneratedPassword()
    {
        var properties = typeof(Response).GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(x => x.Name).OrderBy(x => x).ToArray();

        Assert.Equal(["GeneratedPassword", "Id", "Name", "Roles", "TenantId"], properties);
        Assert.DoesNotContain(properties, x => x.Contains("Hash", StringComparison.OrdinalIgnoreCase) || x.Contains("Salt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FromResultWithSuppliedPasswordDoesNotEchoIt()
    {
        var response = Response.FromResult(new CreateUserResult(StoredUser, "supplied-secret", IsPasswordGenerated: false));

        Assert.Null(response.GeneratedPassword);
        Assert.Equal("user-1", response.Id);
        Assert.Equal("alice", response.Name);
        Assert.Equal(["admin"], response.Roles);
        Assert.Equal("tenant-a", response.TenantId);
    }

    [Fact]
    public void FromResultWithGeneratedPasswordReturnsItOnce()
    {
        var response = Response.FromResult(new CreateUserResult(StoredUser, "generated-secret", IsPasswordGenerated: true));

        Assert.Equal("generated-secret", response.GeneratedPassword);
    }

    [Theory]
    [InlineData("supplied-secret", false, null)]
    [InlineData(null, true, "generated-secret")]
    public async Task EndpointNeverSerializesCredentialMaterial(string? suppliedPassword, bool generated, string? expectedGeneratedPassword)
    {
        var plainText = suppliedPassword ?? "generated-secret";
        var userManager = Substitute.For<IUserManager>();
        userManager.CreateUserAsync("alice", suppliedPassword, Arg.Any<ICollection<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new CreateUserResult(StoredUser, plainText, generated));
        var roleAuthorization = Substitute.For<IRoleAuthorizationService>();
        roleAuthorization.CanAssignRolesAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>()).Returns(true);
        var body = new MemoryStream();
        var endpoint = Factory.Create<Create>(context => context.Response.Body = body, userManager, roleAuthorization);

        await endpoint.HandleAsync(new Request { Name = "alice", Password = suppliedPassword, Roles = ["admin"] }, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, endpoint.HttpContext.Response.StatusCode);
        Assert.Equal(expectedGeneratedPassword, endpoint.Response.GeneratedPassword);
        var json = System.Text.Encoding.UTF8.GetString(body.ToArray());
        Assert.DoesNotContain("hash-must-not-leak", json);
        Assert.DoesNotContain("salt-must-not-leak", json);
        Assert.DoesNotContain("supplied-secret", json);
        if (expectedGeneratedPassword is not null)
            Assert.Contains(expectedGeneratedPassword, json);
    }
}

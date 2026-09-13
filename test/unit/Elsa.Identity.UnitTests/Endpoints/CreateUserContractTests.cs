using System.Reflection;
using Elsa.Identity.Contracts;
using Elsa.Identity.Endpoints.Users.Create;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Threading.Tasks;

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

    [Test]
    public async Task ResponseExposesOnlyAccountFieldsAndTheGeneratedPassword()
    {
        var properties = typeof(Response).GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(x => x.Name).OrderBy(x => x).ToArray();

        await Assert.That(properties).IsEquivalentTo(
            ["GeneratedPassword", "Id", "Name", "Roles", "TenantId"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(properties).DoesNotContain(x => x.Contains("Hash", StringComparison.OrdinalIgnoreCase) || x.Contains("Salt", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task FromResultWithSuppliedPasswordDoesNotEchoIt()
    {
        var response = Response.FromResult(new CreateUserResult(StoredUser, "supplied-secret", IsPasswordGenerated: false));

        await Assert.That(response.GeneratedPassword).IsNull();
        await Assert.That(response.Id).IsEqualTo("user-1");
        await Assert.That(response.Name).IsEqualTo("alice");
        await Assert.That(response.Roles).IsEquivalentTo(["admin"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(response.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    public async Task FromResultWithGeneratedPasswordReturnsItOnce()
    {
        var response = Response.FromResult(new CreateUserResult(StoredUser, "generated-secret", IsPasswordGenerated: true));

        await Assert.That(response.GeneratedPassword).IsEqualTo("generated-secret");
    }

    [Test]
    [Arguments("supplied-secret", false, null)]
    [Arguments(null, true, "generated-secret")]
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

        await Assert.That(endpoint.HttpContext.Response.StatusCode).IsEqualTo(StatusCodes.Status200OK);
        await Assert.That(endpoint.Response.GeneratedPassword).IsEqualTo(expectedGeneratedPassword);
        var json = System.Text.Encoding.UTF8.GetString(body.ToArray());
        await Assert.That(json).DoesNotContain("hash-must-not-leak");
        await Assert.That(json).DoesNotContain("salt-must-not-leak");
        await Assert.That(json).DoesNotContain("supplied-secret");
        if (expectedGeneratedPassword is not null)
            await Assert.That(json).Contains(expectedGeneratedPassword);
    }
}
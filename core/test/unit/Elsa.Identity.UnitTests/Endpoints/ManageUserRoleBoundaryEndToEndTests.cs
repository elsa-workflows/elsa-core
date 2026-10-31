using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Elsa;
using Elsa.Authorization;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Features;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Models;
using Elsa.Identity.Permissions;
using Elsa.UnitTests.Shared;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Endpoints;

/// <summary>
/// End to end through the HTTP pipeline with the real role check: <c>PUT</c> and <c>DELETE /identity/users/{id}</c>
/// only act on users whose current (existing) roles the caller could assign.
/// </summary>
[Collection(nameof(FastEndpointsCollection))]
public sealed class ManageUserRoleBoundaryEndToEndTests : IAsyncLifetime
{
    private readonly User Admin = NewUser("admin-id", "admin", "admin");
    private readonly User OtherAdmin = NewUser("other-admin-id", "other-admin", "admin");
    private readonly User Manager = NewUser("manager-id", "manager", "manager");
    private readonly User Peer = NewUser("peer-id", "peer", "manager");
    private readonly User OrphanedRoleUser = NewUser("orphan-id", "orphan", "deleted-role");
    private readonly User AdminWithOrphanedRole = NewUser("admin-orphan-id", "admin-orphan", "admin", "deleted-role");
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        var module = Substitute.For<IModule>();
        module.Services.Returns(builder.Services);
        new IdentityFeature(module) { TokenOptions = options => options.SigningKey = IdentityTokenTestConstants.SigningKey }.Apply();
        new DefaultAuthenticationFeature(module).Apply();

        foreach (var diagnostic in builder.Services.Where(x => x.ImplementationType?.Namespace == typeof(StoredPermissionValidator).Namespace).ToList())
        {
            builder.Services.Remove(diagnostic);
        }

        builder.Services
            .AddSingleton<Elsa.Common.ISystemClock, Elsa.Common.Services.SystemClock>()
            .AddElsaAuthorization()
            .AddPermissionDescriptors<IdentityPermissionsDescriptorProvider>()
            .AddScoped<IUserCredentialsValidator, NameOnlyCredentialsValidator>()
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(IdentityFeature).Assembly];
                options.Filter = x => x.Namespace is "Elsa.Identity.Endpoints.Login"
                    or "Elsa.Identity.Endpoints.Users.Update"
                    or "Elsa.Identity.Endpoints.Users.Delete";
            });

        _app = builder.Build();
        var users = _app.Services.GetRequiredService<MemoryStore<User>>();
        var roles = _app.Services.GetRequiredService<MemoryStore<Role>>();
        roles.Save(new Role { Id = "admin", Name = "admin", Permissions = [PermissionNames.All] }, x => x.Id);
        roles.Save(new Role { Id = "manager", Name = "manager", Permissions = ["identity/users:update", "identity/users:delete"] }, x => x.Id);
        foreach (var user in new[] { Admin, Manager, OtherAdmin, Peer, OrphanedRoleUser, AdminWithOrphanedRole })
            users.Save(user, x => x.Id);
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        FastEndpointsResolver.Reset();
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task ManagerCannotChangeAnAdminsPassword()
    {
        Assert.Equal(HttpStatusCode.Forbidden, await SendAsync(Manager, HttpMethod.Put, OtherAdmin, new { password = "taken-over" }));
        Assert.Equal("original-hash", FindUser(OtherAdmin)!.HashedPassword);
    }

    [Fact]
    public async Task ManagerCannotDeleteAnAdmin()
    {
        Assert.Equal(HttpStatusCode.Forbidden, await SendAsync(Manager, HttpMethod.Delete, OtherAdmin));
        Assert.NotNull(FindUser(OtherAdmin));
    }

    [Fact]
    public async Task ManagerCanChangeAPeersPassword()
    {
        Assert.Equal(HttpStatusCode.OK, await SendAsync(Manager, HttpMethod.Put, Peer, new { password = "new-password" }));
        Assert.NotEqual("original-hash", FindUser(Peer)!.HashedPassword);
    }

    [Fact]
    public async Task AdminCanDeleteAnotherAdmin()
    {
        Assert.Equal(HttpStatusCode.NoContent, await SendAsync(Admin, HttpMethod.Delete, OtherAdmin));
        Assert.Null(FindUser(OtherAdmin));
    }

    [Fact]
    public async Task RoleIdsOfDeletedRolesDoNotBlockManagement()
    {
        Assert.Equal(HttpStatusCode.OK, await SendAsync(Manager, HttpMethod.Put, OrphanedRoleUser, new { password = "new-password" }));
        Assert.Equal(HttpStatusCode.NoContent, await SendAsync(Admin, HttpMethod.Delete, OrphanedRoleUser));
    }

    [Fact]
    public async Task RoleIdsOfDeletedRolesDoNotHideTheRemainingRoles()
    {
        Assert.Equal(HttpStatusCode.Forbidden, await SendAsync(Manager, HttpMethod.Put, AdminWithOrphanedRole, new { password = "taken-over" }));
        Assert.Equal(HttpStatusCode.OK, await SendAsync(Admin, HttpMethod.Put, AdminWithOrphanedRole, new { password = "new-password" }));
    }

    private static User NewUser(string id, string name, params string[] roles) =>
        new() { Id = id, Name = name, Roles = roles.ToList(), HashedPassword = "original-hash", HashedPasswordSalt = "original-salt" };

    private async Task<IssuedTokens> LoginAsync(User user) =>
        await ReadTokensAsync(await _client.PostAsJsonAsync("/identity/login", new { username = user.Name, password = "unchecked" }));

    private async Task<HttpStatusCode> SendAsync(User caller, HttpMethod method, User target, object? body = null)
    {
        using var request = new HttpRequestMessage(method, $"/identity/users/{target.Id}");
        request.Headers.Authorization = new("Bearer", (await LoginAsync(caller)).AccessToken);
        if (body != null)
            request.Content = JsonContent.Create(body);
        return (await _client.SendAsync(request)).StatusCode;
    }

    private User? FindUser(User user) => _app.Services.GetRequiredService<MemoryStore<User>>().Find(x => x.Id == user.Id);

    private static async Task<IssuedTokens> ReadTokensAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.True(body.IsAuthenticated);
        return new(body.AccessToken!, body.RefreshToken!);
    }

    private sealed class NameOnlyCredentialsValidator(IUserProvider userProvider) : IUserCredentialsValidator
    {
        public async ValueTask<User?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default) =>
            await userProvider.FindByNameAsync(username, cancellationToken);
    }
}

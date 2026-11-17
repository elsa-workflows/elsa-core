using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.Tenants.Endpoints.Tenants.Add;
using Elsa.Tenants.Endpoints.Tenants.Update;
using Elsa.UnitTests.Shared;
using Elsa.Workflows;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using AddEndpoint = Elsa.Tenants.Endpoints.Tenants.Add.Endpoint;
using DeleteEndpoint = Elsa.Tenants.Endpoints.Tenants.Delete.Endpoint;
using GetEndpoint = Elsa.Tenants.Endpoints.Tenants.Get.Endpoint;
using ListEndpoint = Elsa.Tenants.Endpoints.Tenants.List.Endpoint;
using RefreshEndpoint = Elsa.Tenants.Endpoints.Tenants.Refresh.Endpoint;
using UpdateEndpoint = Elsa.Tenants.Endpoints.Tenants.Update.Endpoint;

namespace Elsa.Tenants.UnitTests.Endpoints;

[Collection(nameof(FastEndpointsCollection))]
public class TenantAdministrationTests
{
    private readonly ITenantService _tenants = Substitute.For<ITenantService>();
    private readonly ITenantStore _store = Substitute.For<ITenantStore>();
    private readonly IIdentityGenerator _ids = Substitute.For<IIdentityGenerator>();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TenantWildcardCannotListOrMutateTheHostRegistry(bool includesHostClaim)
    {
        var claims = new List<Claim> { new(PermissionNames.ClaimType, PermissionNames.All), new(TenantPrincipalClaims.BoundTenant, "tenant-a") };
        if (includesHostClaim)
            claims.Add(new(TenantPrincipalClaims.HostAdministrator, "true"));

        var actor = new ClaimsPrincipal(new ClaimsIdentity(claims, "tenant"));
        void Configure(DefaultHttpContext context) => ConfigureContext(context, actor, "tenant-b");
        var add = Factory.Create<AddEndpoint>(Configure, _tenants, _ids, _store);
        var delete = Factory.Create<DeleteEndpoint>(Configure, _tenants, _store);
        var get = Factory.Create<GetEndpoint>(Configure, _tenants);
        var list = Factory.Create<ListEndpoint>(Configure, _tenants);
        var refresh = Factory.Create<RefreshEndpoint>(Configure, _tenants);
        var update = Factory.Create<UpdateEndpoint>(Configure, _tenants, _store);

        await add.HandleAsync(new NewTenant { Id = "tenant-c", Name = "C" }, CancellationToken.None);
        await delete.HandleAsync(CancellationToken.None);
        await get.HandleAsync(CancellationToken.None);
        await list.HandleAsync(CancellationToken.None);
        await refresh.HandleAsync(CancellationToken.None);
        await update.HandleAsync(new UpdatedTenant { Id = "tenant-b", Name = "B" }, CancellationToken.None);

        Assert.All(new[] { add.HttpContext, delete.HttpContext, get.HttpContext, list.HttpContext, refresh.HttpContext, update.HttpContext },
            context => Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode));
        Assert.Empty(_tenants.ReceivedCalls());
        Assert.Empty(_store.ReceivedCalls());
    }

    [Fact]
    public async Task HostOperatorCanEditNamedAndDefaultTenantsWithoutChangingOwnership()
    {
        _store.FindAsync("tenant-a").Returns(new Tenant { Id = "tenant-a", Name = "Tenant A" });
        _store.FindAsync(Tenant.DefaultTenantId).Returns(Tenant.Default);
        var named = Factory.Create<UpdateEndpoint>(context => ConfigureContext(context, HostActor(), "tenant-a"), _tenants, _store);
        var defaultTenant = Factory.Create<UpdateEndpoint>(context => ConfigureContext(context, HostActor(), "-"), _tenants, _store);

        await named.HandleAsync(new UpdatedTenant { Id = "tenant-a", Name = "Updated A" }, CancellationToken.None);
        await defaultTenant.HandleAsync(new UpdatedTenant { Id = "-", Name = "Updated default" }, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, named.HttpContext.Response.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, defaultTenant.HttpContext.Response.StatusCode);
        await _store.Received(1).UpdateAsync(Arg.Is<Tenant>(tenant => tenant.Id == "tenant-a" && tenant.Name == "Updated A" && tenant.TenantId == null));
        await _store.Received(1).UpdateAsync(Arg.Is<Tenant>(tenant => tenant.Id == Tenant.DefaultTenantId && tenant.Name == "Updated default" && tenant.TenantId == null));
        await _tenants.Received(2).RefreshAsync();
    }

    [Fact]
    public async Task HostOperatorCannotDeleteDefaultTenantOrReassignOwnership()
    {
        _store.FindAsync("tenant-a").Returns(new Tenant { Id = "tenant-a", Name = "Tenant A" });
        var update = Factory.Create<UpdateEndpoint>(context => ConfigureContext(context, HostActor(), "tenant-a"), _tenants, _store);
        var delete = Factory.Create<DeleteEndpoint>(context => ConfigureContext(context, HostActor(), "-"), _tenants, _store);

        await update.HandleAsync(new UpdatedTenant { Id = "tenant-a", Name = "Tenant A", TenantId = "tenant-b" }, CancellationToken.None);
        await delete.HandleAsync(CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, update.HttpContext.Response.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, delete.HttpContext.Response.StatusCode);
        await _store.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
        await _store.DidNotReceiveWithAnyArgs().DeleteAsync(default(string)!);
        await _tenants.DidNotReceiveWithAnyArgs().RefreshAsync();
    }

    private static ClaimsPrincipal HostActor() => new(new ClaimsIdentity(
        [new Claim(PermissionNames.ClaimType, PermissionNames.All), new Claim(TenantPrincipalClaims.HostAdministrator, "true")], "host"));

    private static void ConfigureContext(DefaultHttpContext context, ClaimsPrincipal user, string routeId)
    {
        context.User = user;
        context.Request.RouteValues["id"] = routeId;
        context.Response.Body = new MemoryStream();
    }
}

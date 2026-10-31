using Elsa.Common.Multitenancy;
using Elsa.Secrets.Models;
using Elsa.Secrets.Repositories;
using Elsa.Secrets.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using DeleteEndpoint = Elsa.Secrets.Endpoints.Secrets.Delete.Endpoint;
using RevokeEndpoint = Elsa.Secrets.Endpoints.Secrets.Revoke.Endpoint;
using RotateEndpoint = Elsa.Secrets.Endpoints.Secrets.Rotate.Endpoint;
using UpdateEndpoint = Elsa.Secrets.Endpoints.Secrets.Update.Endpoint;

namespace Elsa.Secrets.UnitTests;

/// <summary>
/// A tenant sees a tenant-agnostic (<c>*</c>) secret but cannot write it. Every write endpoint answers 403 with the
/// reason instead of failing with 500 (#8759).
/// </summary>
public class AgnosticSecretWriteEndpointsTests
{
    private const string SecretName = "shared:secret";
    private readonly DefaultTenantAccessor _tenantAccessor = new();
    private readonly DefaultSecretManager _manager;

    public AgnosticSecretWriteEndpointsTests()
    {
        var fixture = new SecretTestFixture();
        _manager = new DefaultSecretManager(new DefaultSecretNameValidator(), fixture.StoreRegistry, fixture.TypeRegistry, new InMemorySecretRepository(_tenantAccessor));
    }

    [Theory]
    [InlineData("update")]
    [InlineData("revoke")]
    [InlineData("delete")]
    [InlineData("rotate")]
    public async Task WritingAnAgnosticSecretFromATenant_Returns403(string operation)
    {
        using (UseTenant(Tenant.AgnosticTenantId))
            await _manager.CreateAsync(new CreateSecretRequest { Name = SecretName, Value = "shared" });

        using (UseTenant("tenant-a"))
        {
            var endpoint = await HandleAsync(operation);

            Assert.Equal(StatusCodes.Status403Forbidden, endpoint.HttpContext.Response.StatusCode);
            Assert.Equal($"A secret named '{SecretName}' belongs to another tenant.", Assert.Single(endpoint.ValidationFailures).ErrorMessage);
        }
    }

    private async Task<BaseEndpoint> HandleAsync(string operation)
    {
        switch (operation)
        {
            case "update":
                var update = CreateEndpoint<UpdateEndpoint>();
                await update.HandleAsync(new UpdateSecretRequest { DisplayName = "Rehomed" }, CancellationToken.None);
                return update;
            case "revoke":
                var revoke = CreateEndpoint<RevokeEndpoint>();
                await revoke.HandleAsync(CancellationToken.None);
                return revoke;
            case "delete":
                var delete = CreateEndpoint<DeleteEndpoint>();
                await delete.HandleAsync(CancellationToken.None);
                return delete;
            default:
                var rotate = CreateEndpoint<RotateEndpoint>();
                await rotate.HandleAsync(new RotateSecretRequest { Value = "rotated" }, CancellationToken.None);
                return rotate;
        }
    }

    private TEndpoint CreateEndpoint<TEndpoint>() where TEndpoint : class, IEndpoint =>
        Factory.Create<TEndpoint>(context =>
        {
            context.Request.RouteValues["name"] = SecretName;
            context.Response.Body = new MemoryStream();
        }, _manager);

    private IDisposable UseTenant(string tenantId) => _tenantAccessor.PushContext(new Tenant { Id = tenantId, Name = tenantId });
}

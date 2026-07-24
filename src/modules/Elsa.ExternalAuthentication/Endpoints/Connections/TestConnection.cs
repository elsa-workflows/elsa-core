using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.Endpoints.Connections;

internal sealed class TestConnection(IServiceProvider services, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/external-authentication/connections/{connectionId}/test");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsTest);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        if (!ConnectionEndpointSupport.TryGetExpectedRevision(HttpContext, out var revision))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status428PreconditionRequired, "precondition_required", "If-Match with the current connection revision is required.", cancellationToken);
            return;
        }

        var tests = services.GetService<ConnectionTestService>();
        if (tests is null)
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status503ServiceUnavailable, "test_unavailable", "The connection test service is unavailable.", cancellationToken);
            return;
        }
        var result = await tests.TestAsync(Route<string>("connectionId")!, revision, tenantAccessor.TenantId, User, cancellationToken);
        switch (result)
        {
            case ConnectionTestOperationResult.Completed(var observation):
                await HttpContext.Response.WriteAsJsonAsync(new ConnectionTestResponse(
                    observation.Status.ToString().ToLowerInvariant(), observation.ObservedAt, observation.TestedMaterialRevision,
                    observation.Category, observation.Summary, observation.Warnings, observation.Duration, observation.CorrelationId), cancellationToken);
                return;
            case ConnectionTestOperationResult.PreconditionFailed(var currentRevision):
                await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status412PreconditionFailed, "precondition_failed", "The connection has changed. Reload it before trying again.", new { currentRevision }, cancellationToken);
                return;
            case ConnectionTestOperationResult.NotFound:
                await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status404NotFound, "not_found", "The connection was not found.", cancellationToken);
                return;
            default:
                await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status400BadRequest, "test_unavailable", "The connection cannot be tested by this deployment.", cancellationToken);
                return;
        }
    }
}

internal sealed record ConnectionTestResponse(string Status, DateTimeOffset ObservedAt, string TestedMaterialRevision, string Category, string Summary, IReadOnlyCollection<string> Warnings, TimeSpan Duration, string CorrelationId);

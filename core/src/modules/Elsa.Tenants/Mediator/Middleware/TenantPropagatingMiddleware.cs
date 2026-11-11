using Elsa.Common.Multitenancy;
using Elsa.Mediator.Middleware.Command;
using Elsa.Mediator.Middleware.Command.Contracts;
using Elsa.Tenants.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.Mediator.Middleware;

/// <summary>
/// Middleware that ensures tenant context is propagated through the request pipeline.
/// </summary>
[UsedImplicitly]
public class TenantPropagatingMiddleware(CommandMiddlewareDelegate next, ITenantScopeFactory tenantScopeFactory, ITenantService tenantService, ITenantAccessor tenantAccessor, IOptions<MultitenancyOptions> options) : ICommandMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(CommandContext context)
    {
        if (context.Headers.TryGetValue(TenantHeaders.TenantIdKey, out var tenantIdVal))
        {
            var tenantId = (string)tenantIdVal;
            var tenant = await tenantService.FindAsync(tenantId);

            if (tenant == null && options.Value.RejectUnresolvedTenants)
                throw new InvalidOperationException($"Tenant '{tenantId}' could not be found. The command was not run.");

            await using var tenantScope = tenantScopeFactory.CreateScope(tenant);
            await next(context);
            return;
        }

        // A command without a tenant ID still runs when it is sent from inside a tenant scope.
        if (options.Value.RejectUnresolvedTenants && tenantAccessor.Tenant == null)
            throw new InvalidOperationException("The command has no tenant ID and was not sent from a tenant scope. The command was not run.");

        await next(context);
    }
}
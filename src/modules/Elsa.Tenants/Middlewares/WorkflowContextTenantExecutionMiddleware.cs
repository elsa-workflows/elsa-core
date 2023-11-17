using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Middleware.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Middlewares;

/// <summary>
/// Middleware that loads and save workflow's tenant into ITenantAccessor. 
/// </summary>
public class WorkflowContextTenantExecutionMiddleware : WorkflowExecutionMiddleware
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <inheritdoc />
    public WorkflowContextTenantExecutionMiddleware(WorkflowMiddlewareDelegate next, IServiceScopeFactory serviceScopeFactory) : base(next)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke each workflow context provider.
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            // Check if this is a background execution.
            var isBackgroundExecution = context.TransientProperties.GetValueOrDefault<object, bool>(BackgroundActivityInvokerMiddleware.IsBackgroundExecution);

            if (!isBackgroundExecution)
            {
                var tenantAccessor = ActivatorUtilities.GetServiceOrCreateInstance<ITenantAccessor>(scope.ServiceProvider);
                string? tenantId = context.Workflow.WorkflowMetadata.TenantId;
                tenantAccessor.SetCurrentTenantId(tenantId);
            }

            // Invoke the next middleware.
            await Next(context);
        }
    }
}
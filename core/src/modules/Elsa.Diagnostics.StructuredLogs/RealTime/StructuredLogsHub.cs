using Elsa.Authorization;
using Elsa.Common.Multitenancy;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Diagnostics.StructuredLogs.RealTime;

[Authorize]
public class StructuredLogsHub(StructuredLogSubscriptionManager subscriptionManager, ITenantAccessor? tenantAccessor) : Hub<IStructuredLogsClient>
{
    public const string SourceChangesGroup = "structured-log-source-changes";
    private static readonly Permission ReadStructuredLogs = new(StructuredLogsResourcePermissions.StructuredLogs, CoreVerbs.View);

    public StructuredLogsHub(StructuredLogSubscriptionManager subscriptionManager) : this(subscriptionManager, null)
    {
    }

    public override async Task OnConnectedAsync()
    {
        if (CanReadStructuredLogs())
            await Groups.AddToGroupAsync(Context.ConnectionId, SourceChangesGroup, Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }

    public async Task SubscribeAsync(StructuredLogFilter? filter)
    {
        EnsureCanReadStructuredLogs();
        await subscriptionManager.SubscribeAsync(Context.ConnectionId, ValidateFilter(filter), tenantAccessor?.TenantId, Context.ConnectionAborted);
    }

    public Task UpdateFilterAsync(StructuredLogFilter? filter)
    {
        EnsureCanReadStructuredLogs();
        return subscriptionManager.UpdateFilterAsync(Context.ConnectionId, ValidateFilter(filter), tenantAccessor?.TenantId, Context.ConnectionAborted);
    }
    
    public Task UnsubscribeAsync()
    {
        return subscriptionManager.UnsubscribeAsync(Context.ConnectionId);
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await UnsubscribeAsync();
        await base.OnDisconnectedAsync(exception);
    }
    
    private static StructuredLogFilter ValidateFilter(StructuredLogFilter? filter)
    {
        filter ??= new();
        
        if (filter.From is { } from && filter.To is { } to && from > to)
            throw new HubException("The log filter 'from' timestamp must be earlier than or equal to 'to'.");
        
        return filter;
    }

    private bool CanReadStructuredLogs()
    {
        var user = Context.User;
        return user?.Identity?.IsAuthenticated == true && PermissionEvaluator.Shared.HasPermission(user, ReadStructuredLogs);
    }

    private void EnsureCanReadStructuredLogs()
    {
        if (!CanReadStructuredLogs())
            throw new HubException("Access denied.");
    }
}

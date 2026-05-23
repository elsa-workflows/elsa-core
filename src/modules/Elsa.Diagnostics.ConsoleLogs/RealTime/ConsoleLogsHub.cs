using Elsa.Diagnostics.ConsoleLogs.Permissions;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Diagnostics.ConsoleLogs.RealTime;

[Authorize]
public class ConsoleLogsHub(ConsoleLogSubscriptionManager subscriptionManager) : Hub<IConsoleLogsClient>
{
    private const string ReadAllPermission = "read:*";
    private static readonly string[] ReadPermissions = [PermissionNames.All, ReadAllPermission, ConsoleLogsPermissions.Read];

    public Task SubscribeAsync(ConsoleLogFilter? filter)
    {
        EnsureCanReadConsoleLogs();
        return subscriptionManager.SubscribeAsync(Context.ConnectionId, ValidateFilter(filter), Context.ConnectionAborted);
    }

    public Task UpdateFilterAsync(ConsoleLogFilter? filter)
    {
        EnsureCanReadConsoleLogs();
        return subscriptionManager.UpdateFilterAsync(Context.ConnectionId, ValidateFilter(filter), Context.ConnectionAborted);
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

    private static ConsoleLogFilter ValidateFilter(ConsoleLogFilter? filter)
    {
        filter ??= new();

        if (filter.From is { } from && filter.To is { } to && from > to)
            throw new HubException("The console log filter 'from' timestamp must be earlier than or equal to 'to'.");

        return filter;
    }

    private void EnsureCanReadConsoleLogs()
    {
        var user = Context.User;

        if (user?.Identity?.IsAuthenticated != true || !ReadPermissions.Any(user.HasPermission))
            throw new HubException("Access denied.");
    }
}

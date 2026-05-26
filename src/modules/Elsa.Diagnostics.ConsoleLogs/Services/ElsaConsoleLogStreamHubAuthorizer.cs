using ConsoleLogStreaming.SignalR;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using FastEndpoints.Security;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public sealed class ElsaConsoleLogStreamingHubAuthorizer : IConsoleLogStreamingHubAuthorizer
{
    private const string ReadAllPermission = "read:*";
    private static readonly string[] ReadPermissions = [PermissionNames.All, ReadAllPermission, ConsoleLogsPermissions.Read];

    public ValueTask<bool> CanReadAsync(HubCallerContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = context.User;
        return ValueTask.FromResult(user?.Identity?.IsAuthenticated == true && ReadPermissions.Any(user.HasPermission));
    }
}

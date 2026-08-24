using Elsa.Authorization;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using FastEndpoints.Security;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public interface IElsaConsoleLogHubAuthorizer
{
    ValueTask<bool> CanReadAsync(HubCallerContext context, CancellationToken cancellationToken = default);
}

public sealed class ElsaConsoleLogStreamHubAuthorizer : IElsaConsoleLogHubAuthorizer
{
    private static readonly Permission ReadConsoleLogs = new(ConsoleLogsResourcePermissions.ConsoleLogs, CoreVerbs.View);

    public ValueTask<bool> CanReadAsync(HubCallerContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = context.User;
        return ValueTask.FromResult(user?.Identity?.IsAuthenticated == true && PermissionEvaluator.Shared.HasPermission(user, ReadConsoleLogs));
    }
}

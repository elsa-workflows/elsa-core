using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Notifications;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Endpoints.Sessions;

internal sealed class ListExternalAuthenticationSessions(
    IExternalAuthenticationSessionStore sessions,
    ITenantAccessor tenantAccessor,
    IOptions<ExternalAuthenticationOptions> options) : ElsaEndpoint<ExternalAuthenticationSessionListRequest, ExternalAuthenticationSessionListResponse>
{
    public override void Configure()
    {
        Get("/external-authentication/sessions");
        ConfigurePermissions(ExternalAuthenticationPermissions.SessionsRead);
    }

    public override async Task<ExternalAuthenticationSessionListResponse> ExecuteAsync(ExternalAuthenticationSessionListRequest request, CancellationToken cancellationToken)
    {
        if (!options.Value.Operations.EnableSessionAdministration)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return new ExternalAuthenticationSessionListResponse([], null);
        }
        if (request.PageSize is < 1 or > 100 || !IsKnownStatus(request.Status) || !TryDecode(request.Cursor, out var cursor))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return new ExternalAuthenticationSessionListResponse([], null);
        }
        var pageSize = request.PageSize ?? 100;
        var rows = (await sessions.FindAsync(new Models.ExternalAuthenticationSessionFilter { TenantId = tenantAccessor.TenantId, UserId = request.UserId, ConnectionId = request.ConnectionId, Status = request.Status }, cancellationToken))
            .OrderBy(x => x.StartedAt).ThenBy(x => x.Id, StringComparer.Ordinal)
            .Where(x => cursor is null || Compare(new SessionCursor(x.StartedAt, x.Id), cursor) > 0)
            .Take(pageSize + 1).ToArray();
        var hasMore = rows.Length > pageSize;
        var page = hasMore ? rows[..^1] : rows;
        return new ExternalAuthenticationSessionListResponse(page.Select(ExternalAuthenticationSessionDocument.From).ToArray(), hasMore ? Encode(new SessionCursor(page[^1].StartedAt, page[^1].Id)) : null);
    }

    private static bool IsKnownStatus(string? status) => string.IsNullOrWhiteSpace(status) || status.Equals("active", StringComparison.OrdinalIgnoreCase) || status.Equals("revoked", StringComparison.OrdinalIgnoreCase);
    private static int Compare(SessionCursor left, SessionCursor right) => left.StartedAt != right.StartedAt ? left.StartedAt.CompareTo(right.StartedAt) : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    internal static string Encode(SessionCursor cursor) => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cursor))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    internal static bool TryDecode(string? value, out SessionCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            cursor = JsonSerializer.Deserialize<SessionCursor>(Encoding.UTF8.GetString(Convert.FromBase64String(padded)));
            return cursor is { Id.Length: > 0 and <= 256 };
        }
        catch (Exception exception) when (exception is FormatException or JsonException) { return false; }
    }
}

internal sealed class RevokeExternalAuthenticationSession(
    IExternalAuthenticationSessionStore sessions,
    ITenantAccessor tenantAccessor,
    IOptions<ExternalAuthenticationOptions> options,
    ExternalAuthenticationSecurityNotifier notifier) : ElsaEndpoint<RevokeExternalAuthenticationSessionRequest>
{
    public override void Configure()
    {
        Delete("/external-authentication/sessions/{sessionId}");
        ConfigurePermissions(ExternalAuthenticationPermissions.SessionsRevoke);
    }

    public override async Task HandleAsync(RevokeExternalAuthenticationSessionRequest request, CancellationToken cancellationToken)
    {
        if (!options.Value.Operations.EnableSessionAdministration)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        var session = await sessions.FindByIdAsync(Route<string>("sessionId")!, cancellationToken);
        if (session is null || !string.Equals(session.TenantId, tenantAccessor.TenantId, StringComparison.Ordinal))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        var revoked = await sessions.RevokeAsync(session.Id, string.IsNullOrWhiteSpace(request.Reason) ? "administrator_revoked" : request.Reason[..Math.Min(request.Reason.Length, 128)], DateTimeOffset.UtcNow, cancellationToken);
        if (!revoked)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }
        await notifier.PublishAsync(new ExternalAuthenticationSessionRevoked(
            ExternalAuthenticationSecurityNotifier.Context(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value, tenantAccessor.TenantId, session.ConnectionId, session.UserId, SecurityEventOutcome.Succeeded, "External authentication session revoked."),
            session.Id,
            "administrator_revoked"), cancellationToken);
        HttpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }
}

internal sealed class ExternalAuthenticationSessionListRequest
{
    public string? UserId { get; set; }
    public string? ConnectionId { get; set; }
    public string? Status { get; set; }
    public string? Cursor { get; set; }
    public int? PageSize { get; set; }
}
internal sealed class RevokeExternalAuthenticationSessionRequest { public string? Reason { get; set; } }
internal sealed record SessionCursor(DateTimeOffset StartedAt, string Id);
internal sealed record ExternalAuthenticationSessionListResponse(IReadOnlyCollection<ExternalAuthenticationSessionDocument> Items, string? NextCursor);
internal sealed record ExternalAuthenticationSessionDocument(string Id, string UserId, string TenantId, string ConnectionId, DateTimeOffset StartedAt, DateTimeOffset LastRefreshedAt, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt, string Status)
{
    public static ExternalAuthenticationSessionDocument From(Models.ExternalAuthenticationSession value) => new(value.Id, value.UserId, value.TenantId, value.ConnectionId, value.StartedAt, value.LastRefreshedAt, value.ExpiresAt, value.RevokedAt, value.RevokedAt is null ? "active" : "revoked");
}

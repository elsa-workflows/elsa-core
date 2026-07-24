using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.ExternalAuthentication.Endpoints.IdentityLinks;

internal sealed class GetIdentityLinks(ExternalIdentityLinkManagementService management, ITenantAccessor tenantAccessor) : ElsaEndpoint<IdentityLinkListRequest, IdentityLinkListResponse>
{
    public override void Configure()
    {
        Get("/external-authentication/identity-links");
        ConfigurePermissions(ExternalAuthenticationPermissions.LinksManage);
    }

    public override async Task<IdentityLinkListResponse> ExecuteAsync(IdentityLinkListRequest request, CancellationToken cancellationToken)
    {
        if (request.PageSize is < 1 or > 100 || !IdentityLinkPagination.TryDecodeCursor(request.Cursor, out var cursor))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return new IdentityLinkListResponse([], null);
        }

        var pageSize = request.PageSize ?? 100;
        var links = (await management.ListAsync(tenantAccessor.TenantId, new ExternalIdentityLinkFilter { UserId = request.UserId, ConnectionId = request.ConnectionId }, cancellationToken)).Items
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .Where(x => cursor is null || IdentityLinkPagination.Compare(new IdentityLinkCursor(x.CreatedAt, x.Id), cursor) > 0)
            .Take(pageSize + 1)
            .ToArray();
        var hasMore = links.Length > pageSize;
        var items = hasMore ? links[..^1] : links;
        return new IdentityLinkListResponse(items.Select(IdentityLinkDocument.From).ToArray(), hasMore ? IdentityLinkPagination.EncodeCursor(new IdentityLinkCursor(items[^1].CreatedAt, items[^1].Id)) : null);
    }
}

internal sealed class FindUsers(ExternalIdentityLinkManagementService management, ITenantAccessor tenantAccessor) : ElsaEndpoint<FindIdentityLinkUsersRequest, FindIdentityLinkUsersResponse>
{
    public override void Configure()
    {
        Get("/external-authentication/user-options");
        ConfigurePermissions(ExternalAuthenticationPermissions.LinksManage);
    }

    public override async Task<FindIdentityLinkUsersResponse> ExecuteAsync(FindIdentityLinkUsersRequest request, CancellationToken cancellationToken)
    {
        if (request.PageSize is < 1 or > 50 || !IdentityLinkPagination.TryDecodeUserCursor(request.Cursor, out var cursor))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return new FindIdentityLinkUsersResponse([], null);
        }

        var pageSize = request.PageSize ?? 50;
        var users = (await management.FindUsersAsync(tenantAccessor.TenantId, request.Search, cancellationToken)).Items
            .Where(x => cursor is null || IdentityLinkPagination.Compare(new UserCursor(x.DisplayName, x.Id), cursor) > 0)
            .Take(pageSize + 1)
            .ToArray();
        var hasMore = users.Length > pageSize;
        var items = hasMore ? users[..^1] : users;
        return new FindIdentityLinkUsersResponse(items.Select(x => new IdentityLinkUserDocument(x.Id, x.DisplayName)).ToArray(), hasMore ? IdentityLinkPagination.EncodeUserCursor(new UserCursor(items[^1].DisplayName, items[^1].Id)) : null);
    }
}

internal sealed class PrelinkIdentityLink(ExternalIdentityLinkManagementService management, ITenantAccessor tenantAccessor) : ElsaEndpoint<PrelinkIdentityLinkRequest>
{
    public override void Configure()
    {
        Post("/external-authentication/identity-links");
        ConfigurePermissions(ExternalAuthenticationPermissions.LinksManage);
    }

    public override async Task HandleAsync(PrelinkIdentityLinkRequest request, CancellationToken cancellationToken)
    {
        ExternalIdentityLinkPrelinkResult result;
        try
        {
            result = await management.PrelinkAsync(tenantAccessor.TenantId, request.UserId, request.ConnectionId, request.Issuer, request.Subject, User, cancellationToken);
        }
        catch (ArgumentException)
        {
            await SendErrorAsync(StatusCodes.Status400BadRequest, "validation_failed", "The external identity tuple is invalid.", cancellationToken);
            return;
        }

        switch (result)
        {
            case ExternalIdentityLinkPrelinkResult.Success(var link, var wasCreated):
                HttpContext.Response.StatusCode = wasCreated ? StatusCodes.Status201Created : StatusCodes.Status200OK;
                await HttpContext.Response.WriteAsJsonAsync(IdentityLinkDocument.From(link), cancellationToken);
                return;
            case ExternalIdentityLinkPrelinkResult.Conflict:
                await SendErrorAsync(StatusCodes.Status409Conflict, "conflict", "The external identity is already linked to another user.", cancellationToken);
                return;
            default:
                // Do not reveal whether a user or connection exists outside the trusted tenant scope.
                await SendErrorAsync(StatusCodes.Status404NotFound, "not_found", "The requested resource was not found.", cancellationToken);
                return;
        }
    }

    private Task SendErrorAsync(int status, string error, string message, CancellationToken cancellationToken)
    {
        HttpContext.Response.StatusCode = status;
        return HttpContext.Response.WriteAsJsonAsync(new IdentityLinkError(error, message), cancellationToken);
    }
}

internal sealed class DeleteIdentityLink(ExternalIdentityLinkManagementService management, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/external-authentication/identity-links/{linkId}");
        ConfigurePermissions(ExternalAuthenticationPermissions.LinksManage);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var deleted = await management.UnlinkAsync(tenantAccessor.TenantId, Route<string>("linkId")!, User, cancellationToken);
        HttpContext.Response.StatusCode = deleted ? StatusCodes.Status204NoContent : StatusCodes.Status404NotFound;
    }
}

internal sealed class IdentityLinkListRequest
{
    public string? UserId { get; set; }
    public string? ConnectionId { get; set; }
    public string? Cursor { get; set; }
    public int? PageSize { get; set; }
}

internal sealed class FindIdentityLinkUsersRequest
{
    public string? Search { get; set; }
    public string? Cursor { get; set; }
    public int? PageSize { get; set; }
}

internal sealed class PrelinkIdentityLinkRequest
{
    public string UserId { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Subject { get; set; } = null!;
}

internal sealed record IdentityLinkListResponse(IReadOnlyCollection<IdentityLinkDocument> Items, string? NextCursor);
internal sealed record FindIdentityLinkUsersResponse(IReadOnlyCollection<IdentityLinkUserDocument> Items, string? NextCursor);
internal sealed record IdentityLinkUserDocument(string Id, string DisplayName);
internal sealed record IdentityLinkDocument(string Id, string UserId, string ConnectionId, string Issuer, string? SubjectHint, DateTimeOffset CreatedAt, DateTimeOffset? LastSignedInAt)
{
    public static IdentityLinkDocument From(ExternalIdentityLink link) => new(link.Id, link.UserId, link.ConnectionId, link.Issuer, link.SubjectHint, link.CreatedAt, link.LastSignedInAt);
}
internal sealed record IdentityLinkError(string Error, string Message);
internal sealed record IdentityLinkCursor(DateTimeOffset CreatedAt, string Id);
internal sealed record UserCursor(string DisplayName, string Id);

internal static class IdentityLinkPagination
{
    public static string EncodeCursor(IdentityLinkCursor cursor) => Encode(cursor);
    public static string EncodeUserCursor(UserCursor cursor) => Encode(cursor);
    public static bool TryDecodeCursor(string? value, out IdentityLinkCursor? cursor) => TryDecode(value, out cursor);
    public static bool TryDecodeUserCursor(string? value, out UserCursor? cursor) => TryDecode(value, out cursor);

    public static int Compare(IdentityLinkCursor left, IdentityLinkCursor right)
    {
        var result = left.CreatedAt.CompareTo(right.CreatedAt);
        return result != 0 ? result : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }

    public static int Compare(UserCursor left, UserCursor right)
    {
        var result = string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
        return result != 0 ? result : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }

    private static string Encode<T>(T cursor) => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cursor))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryDecode<T>(string? value, out T? cursor) where T : class
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;
        if (value.Length > 512)
            return false;
        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            cursor = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(Convert.FromBase64String(padded)));
            return cursor is not null && IsBounded(cursor);
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return false;
        }
    }

    private static bool IsBounded<T>(T cursor) where T : class => cursor switch
    {
        IdentityLinkCursor value => value.Id.Length is > 0 and <= 256,
        UserCursor value => value.Id.Length is > 0 and <= 256 && value.DisplayName.Length <= 512,
        _ => false
    };
}

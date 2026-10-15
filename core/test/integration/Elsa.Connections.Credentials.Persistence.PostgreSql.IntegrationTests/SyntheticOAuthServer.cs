using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

public sealed class SyntheticOAuthServer : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly object _gate = new();
    private readonly HashSet<string> _availableRefreshTokens = new(StringComparer.Ordinal);
    private readonly HashSet<string> _consumedRefreshTokens = new(StringComparer.Ordinal);
    private readonly HashSet<string> _revocationOperationIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _uninstallOperationIds = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> _revocationCallIds = new();
    private readonly ConcurrentQueue<string> _uninstallCallIds = new();
    private readonly TaskCompletionSource _refreshEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseRefresh = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly WebApplication _application;
    private int _refreshCalls;
    private int _acceptedRefreshCalls;
    private int _revocationCalls;
    private int _revocationEffects;
    private int _uninstallCalls;
    private int _uninstallEffects;

    private SyntheticOAuthServer(WebApplication application)
    {
        _application = application;
    }

    public string Address { get; private set; } = string.Empty;
    public bool StableRevocationIdempotency { get; set; }
    public bool StableUninstallIdempotency { get; set; }
    public bool BlockRefresh { get; set; }
    public int RefreshCalls => Volatile.Read(ref _refreshCalls);
    public int AcceptedRefreshCalls => Volatile.Read(ref _acceptedRefreshCalls);
    public int RevocationCalls => Volatile.Read(ref _revocationCalls);
    public int RevocationEffects => Volatile.Read(ref _revocationEffects);
    public int UninstallCalls => Volatile.Read(ref _uninstallCalls);
    public int UninstallEffects => Volatile.Read(ref _uninstallEffects);
    public IReadOnlyCollection<string> RevocationCallIds => _revocationCallIds.ToArray();
    public IReadOnlyCollection<string> UninstallCallIds => _uninstallCallIds.ToArray();

    public Task WaitForRefreshAsync(TimeSpan timeout) => _refreshEntered.Task.WaitAsync(timeout);
    public void ReleaseRefresh() => _releaseRefresh.TrySetResult();

    public static async Task<SyntheticOAuthServer> StartAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Logging.SetMinimumLevel(LogLevel.None);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var application = builder.Build();
        var server = new SyntheticOAuthServer(application);
        server.MapEndpoints(application);
        await application.StartAsync();
        var address = application.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        server.Address = address.EndsWith('/') ? address : $"{address}/";
        return server;
    }

    public void IssueRefreshToken(string token)
    {
        lock (_gate)
        {
            if (!_availableRefreshTokens.Add(token))
            {
                throw new InvalidOperationException("synthetic_refresh_token_duplicate");
            }
        }
    }

    public bool IsRefreshTokenConsumed(string token)
    {
        lock (_gate)
        {
            return _consumedRefreshTokens.Contains(token);
        }
    }

    public async ValueTask DisposeAsync() => await _application.DisposeAsync();

    private void MapEndpoints(WebApplication application)
    {
        application.MapPost("/refresh", async (HttpContext context) =>
        {
            Interlocked.Increment(ref _refreshCalls);
            var request = await JsonSerializer.DeserializeAsync<RefreshRequest>(context.Request.Body, JsonOptions, context.RequestAborted);
            if (request is null)
            {
                return Results.BadRequest(new { error = "request_missing" });
            }

            int sequence;
            lock (_gate)
            {
                if (!_availableRefreshTokens.Remove(request.RefreshToken))
                {
                    return Results.BadRequest(new { error = "invalid_grant" });
                }

                _consumedRefreshTokens.Add(request.RefreshToken);
                sequence = Interlocked.Increment(ref _acceptedRefreshCalls);
                _availableRefreshTokens.Add($"refresh-rotated-{sequence}");
            }

            _refreshEntered.TrySetResult();
            if (BlockRefresh)
            {
                await _releaseRefresh.Task.WaitAsync(context.RequestAborted);
            }

            return Results.Ok(new RefreshResponse($"access-rotated-{sequence}", $"refresh-rotated-{sequence}", 3600));
        });

        application.MapPost("/revoke", async (HttpContext context) =>
        {
            var request = await JsonSerializer.DeserializeAsync<OffboardingRequest>(context.Request.Body, JsonOptions, context.RequestAborted);
            if (request is null)
            {
                return Results.BadRequest(new { error = "request_missing" });
            }

            Interlocked.Increment(ref _revocationCalls);
            _revocationCallIds.Enqueue(request.OperationId);
            lock (_gate)
            {
                if (!StableRevocationIdempotency || _revocationOperationIds.Add(request.OperationId))
                {
                    Interlocked.Increment(ref _revocationEffects);
                }
            }

            return Results.Ok(new { outcome = "succeeded" });
        });

        application.MapPost("/uninstall", async (HttpContext context) =>
        {
            var request = await JsonSerializer.DeserializeAsync<OffboardingRequest>(context.Request.Body, JsonOptions, context.RequestAborted);
            if (request is null)
            {
                return Results.BadRequest(new { error = "request_missing" });
            }

            Interlocked.Increment(ref _uninstallCalls);
            _uninstallCallIds.Enqueue(request.OperationId);
            lock (_gate)
            {
                if (!StableUninstallIdempotency || _uninstallOperationIds.Add(request.OperationId))
                {
                    Interlocked.Increment(ref _uninstallEffects);
                }
            }

            return Results.Ok(new { outcome = "succeeded" });
        });
    }

    private sealed record RefreshRequest(string ProviderId, string AccountId, string RefreshToken);
    private sealed record RefreshResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds);
    private sealed record OffboardingRequest(string ProviderId, string ProviderAccountId, string OperationId, string? AccessToken, string? RefreshToken);
}

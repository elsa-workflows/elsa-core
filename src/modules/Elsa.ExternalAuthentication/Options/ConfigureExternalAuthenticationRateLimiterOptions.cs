using System.Threading.RateLimiting;
using Elsa.ExternalAuthentication.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Options;

public sealed class ConfigureExternalAuthenticationRateLimiterOptions(IOptions<ExternalAuthenticationOptions> externalAuthenticationOptions) : IConfigureOptions<RateLimiterOptions>
{
    public void Configure(RateLimiterOptions options)
    {
        var rateLimits = externalAuthenticationOptions.Value.RateLimits;
        AddPolicy(options, ExternalAuthenticationRateLimitPolicyNames.Discovery, rateLimits.Discovery, rateLimits.PartitionStrategy);
        AddPolicy(options, ExternalAuthenticationRateLimitPolicyNames.ExternalInitiation, rateLimits.ExternalInitiation, rateLimits.PartitionStrategy);
        AddPolicy(options, ExternalAuthenticationRateLimitPolicyNames.LocalInitiation, rateLimits.LocalInitiation, rateLimits.PartitionStrategy);
        AddPolicy(options, ExternalAuthenticationRateLimitPolicyNames.ProviderCallback, rateLimits.ProviderCallback, rateLimits.PartitionStrategy);
        AddPolicy(options, ExternalAuthenticationRateLimitPolicyNames.TokenExchange, rateLimits.TokenExchange, rateLimits.PartitionStrategy);

        var previousOnRejected = options.OnRejected;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (previousOnRejected != null)
                await previousOnRejected(context, cancellationToken);
        };
    }

    private static void AddPolicy(RateLimiterOptions options, string name, RateLimitRule rule, ExternalAuthenticationRateLimitPartitionStrategy partitionStrategy)
    {
        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            GetPartitionKey(context, partitionStrategy),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rule.PermitLimit,
                Window = rule.Window,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            }));
    }

    private static string GetPartitionKey(HttpContext context, ExternalAuthenticationRateLimitPartitionStrategy partitionStrategy)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (partitionStrategy != ExternalAuthenticationRateLimitPartitionStrategy.ClientIdAndRemoteIp)
            return remoteIp;

        var clientId = context.Request.Query["client_id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(clientId) ? remoteIp : $"{remoteIp}:{clientId}";
    }
}

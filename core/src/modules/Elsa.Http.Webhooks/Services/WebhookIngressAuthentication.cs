using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Elsa.Common;
using Elsa.Http.Webhooks.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Webhooks.Services;

internal sealed class WebhookIngressAuthentication(IOptions<WebhookIngressOptions> options, ISystemClock clock)
{
    private static readonly TimeSpan Freshness = TimeSpan.FromMinutes(5);

    public WebhookDelivery? ReadDelivery(IHeaderDictionary headers)
    {
        if (!TryReadId(headers, "X-Elsa-Webhook-Source", out var source) ||
            !TryReadId(headers, "X-Elsa-Webhook-Delivery", out var delivery) ||
            !TryReadHeader(headers, "X-Elsa-Webhook-Timestamp", out var timestamp) ||
            !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
            !TryReadHeader(headers, "X-Elsa-Webhook-Signature", out var signature) ||
            signature.Length != 44 ||
            !options.Value.SigningKeys.TryGetValue(source, out var key) ||
            key.Length != 44 ||
            options.Value.SigningKeys.Values.Count(value => string.Equals(value, key, StringComparison.Ordinal)) != 1)
            return null;

        var now = clock.UtcNow;
        if (seconds <= (now - Freshness).ToUnixTimeSeconds() || seconds > (now + Freshness).ToUnixTimeSeconds())
            return null;

        return new WebhookDelivery(source, delivery, timestamp, signature, key, DateTimeOffset.FromUnixTimeSeconds(seconds) + Freshness);
    }

    public static bool Authenticate(WebhookDelivery delivery, ReadOnlySpan<byte> body)
    {
        Span<byte> expected = stackalloc byte[32];
        if (!Convert.TryFromBase64String(delivery.Signature, expected, out var count) || count != expected.Length)
            return false;

        var key = new byte[32];
        try
        {
            if (!Convert.TryFromBase64String(delivery.Key, key, out var keyLength) ||
                keyLength != key.Length ||
                !string.Equals(Convert.ToBase64String(key), delivery.Key, StringComparison.Ordinal))
                return false;

            using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key);
            hmac.AppendData(Encoding.UTF8.GetBytes($"{delivery.SourceId}\n{delivery.DeliveryId}\n{delivery.Timestamp}\n"));
            hmac.AppendData(body);
            return CryptographicOperations.FixedTimeEquals(expected, hmac.GetHashAndReset());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static bool TryReadId(IHeaderDictionary headers, string name, out string value) =>
        TryReadHeader(headers, name, out value) && value.Length is > 0 and <= 128 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-');

    private static bool TryReadHeader(IHeaderDictionary headers, string name, out string value)
    {
        value = "";
        if (!headers.TryGetValue(name, out var values) || values.Count != 1 || values[0] is not { } item)
            return false;

        value = item;
        return true;
    }
}

internal sealed record WebhookDelivery(string SourceId, string DeliveryId, string Timestamp, string Signature, string Key, DateTimeOffset ExpiresAt)
{
    public override string ToString() => nameof(WebhookDelivery);
}

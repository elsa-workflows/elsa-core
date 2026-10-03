using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Elsa.Identity.UnitTests;

/// <summary>
/// Mirrors <c>Elsa.Studio.Authentication.ElsaIdentity.Services.JwtParser</c> on elsa-studio
/// <c>release/3.9.0</c>, which Studio WASM uses to turn an access token into claims.
/// </summary>
internal static class StudioWasmJwtParser
{
    public static IReadOnlyCollection<Claim> Parse(string jwt)
    {
        var claims = new List<Claim>();

        foreach (var property in ReadPayload(jwt).EnumerateObject())
            AddClaims(property.Name, property.Value, claims);

        return claims;
    }

    public static JsonElement ReadPayload(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(padded))).RootElement.Clone();
    }

    private static void AddClaims(string type, JsonElement value, ICollection<Claim> claims)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                    AddClaims(type, item, claims);
                return;
            case JsonValueKind.String:
                claims.Add(new(type, value.GetString() ?? string.Empty));
                return;
            default:
                claims.Add(new(type, value.ToString()));
                return;
        }
    }
}

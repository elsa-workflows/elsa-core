using Elsa.Studio.Login.Contracts;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.Studio.Login.BlazorWasm.Services;

/// <inheritdoc />
public class BlazorWasmJwtParser : IJwtParser
{
    // Taken and adapted from: https://trystanwilcock.com/2022/09/28/net-6-0-blazor-webassembly-jwt-token-authentication-from-scratch-c-sharp-wasm-tutorial/
    /// <inheritdoc />
    public IEnumerable<Claim> Parse(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = WebEncoders.Base64UrlDecode(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonNode>>(jsonBytes)!;
        foreach (var (key, value) in keyValuePairs)
        {
            // Having an ClaimsIdentity with the following claims
            // new Claim("A", "val1");
            // new Claim("A", "val2");
            // is serialized as
            // { "A": ["val1", "val2"] }
            // so handle that here
            if (value is JsonArray array)
            {
                foreach (var arrayValue in array)
                {
                    if (arrayValue != null)
                    {
                        yield return new Claim(key, arrayValue.ToString());
                    }
                }
            }
            else
            {
                yield return new Claim(key, value.ToString());
            }
        }
    }
}
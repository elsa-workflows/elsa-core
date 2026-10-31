using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Studio.Authentication.ElsaIdentity.Models;

/// <summary>The body of the backend's <c>POST /identity/logout</c> request.</summary>
/// <param name="RefreshToken">The refresh token of the session to revoke.</param>
internal sealed record LogoutRequest(string RefreshToken);

/// <summary>Source-generated serialization, so the request body survives trimming.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(LogoutRequest))]
internal sealed partial class ElsaIdentityLogoutJsonContext : JsonSerializerContext;

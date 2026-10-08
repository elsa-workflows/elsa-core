using System.Text.Json.Serialization;

namespace Elsa.Studio.Security.Models;

/// <summary>Trim-safe JSON metadata for structured Identity API failures.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CoreApiErrorResponse))]
[JsonSerializable(typeof(ValidationApiErrorResponse))]
[JsonSerializable(typeof(RoleDeletionImpactResponse))]
internal partial class IdentityJsonSerializerContext : JsonSerializerContext;

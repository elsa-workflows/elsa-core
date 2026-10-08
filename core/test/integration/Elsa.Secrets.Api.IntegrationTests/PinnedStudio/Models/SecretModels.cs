using System.Text.Json.Serialization;

namespace Elsa.Studio.Secrets.Models;

public static class SecretInputUIHints
{
    public const string SecretPicker = "secret-picker";
}

public static class SecretExpressionTypes
{
    public const string Secret = "Secret";
}

public enum SecretStatus
{
    Active,
    Retired,
    Expired,
    Revoked,
    Deleted
}

[Flags]
public enum SecretStoreCapabilities
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4,
    Test = 8,
    ExportEncrypted = 16,
    Versioned = 32
}

public static class SecretStoreNames
{
    public const string Encrypted = "encrypted";
    public const string Configuration = "configuration";
}

public static class SecretTypeNames
{
    public const string Text = "text";
    public const string RsaKey = "rsa-key";
    public const string X509Certificate = "x509-certificate";
}

public class SecretModel
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? Description { get; set; }
    public string TypeName { get; set; } = default!;
    public string StoreName { get; set; } = default!;
    public string? Scope { get; set; }
    public SecretStatus Status { get; set; }
    public int? CurrentVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public record SecretReference(string Name, string? TypeName = null, string? Scope = null);

public class CreateSecretRequest
{
    public string Name { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string TypeName { get; set; } = SecretTypeNames.Text;
    public string StoreName { get; set; } = SecretStoreNames.Encrypted;
    public string? Scope { get; set; }
    public string? Value { get; set; }
    public string? ConfigurationKey { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public class UpdateSecretRequest
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
}

public class RotateSecretRequest
{
    public string? Value { get; set; }
    public string? ConfigurationKey { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public class ListSecretsResponse
{
    public ICollection<SecretModel> Items { get; set; } = [];
    public long TotalCount { get; set; }
}

public record SecretStoreDescriptor(
    string Name,
    string DisplayName,
    string Description,
    SecretStoreCapabilities Capabilities,
    bool IsReadOnly);

public record SecretTypeDescriptor(
    string Name,
    string DisplayName,
    string Description,
    string EditorHint,
    IReadOnlyCollection<string> SupportedStoreNames);

public class SecretDescriptorsResponse
{
    public ICollection<SecretTypeDescriptor> Types { get; set; } = [];
    public ICollection<SecretStoreDescriptor> Stores { get; set; } = [];
}

public class SecretPickerRequest
{
    public string? Search { get; set; }
    public ICollection<string> TypeNames { get; set; } = [];
    public ICollection<string> StoreNames { get; set; } = [];
    public string? Scope { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public class SecretPickerOptions
{
    public ICollection<string> TypeNames { get; set; } = [];
    public ICollection<string> StoreNames { get; set; } = [];
    public string? Scope { get; set; }
    public bool AllowInlineCreate { get; set; } = true;
}

public class SecretPickerResponse
{
    public ICollection<SecretModel> Items { get; set; } = [];
    public bool CanCreateInline { get; set; }
}

public class SecretTestResponse
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
}

[JsonSerializable(typeof(SecretPickerOptions))]
[JsonSerializable(typeof(SecretReference))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true)]
internal partial class SecretJsonSerializerContext : JsonSerializerContext;

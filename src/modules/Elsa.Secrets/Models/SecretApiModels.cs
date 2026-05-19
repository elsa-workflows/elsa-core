namespace Elsa.Secrets.Models;

public class SecretModel
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? Description { get; set; }
    public string TypeName { get; set; } = default!;
    public string StoreName { get; set; } = default!;
    public string? Scope { get; set; }
    public ICollection<string> Tags { get; set; } = [];
    public SecretStatus Status { get; set; }
    public int? CurrentVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class CreateSecretRequest
{
    public string Name { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string TypeName { get; set; } = SecretTypeNames.Text;
    public string StoreName { get; set; } = SecretStoreNames.Encrypted;
    public string? Scope { get; set; }
    public ICollection<string> Tags { get; set; } = [];
    public string? Value { get; set; }
    public string? ConfigurationKey { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public class RotateSecretRequest
{
    public string? Value { get; set; }
    public string? ConfigurationKey { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public class ListSecretsRequest
{
    public string? Search { get; set; }
    public string? TypeName { get; set; }
    public ICollection<string> TypeNames { get; set; } = [];
    public string? StoreName { get; set; }
    public ICollection<string> StoreNames { get; set; } = [];
    public string? Scope { get; set; }
    public SecretStatus? Status { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public class ListSecretsResponse
{
    public ICollection<SecretModel> Items { get; set; } = [];
    public long TotalCount { get; set; }
}

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

public class SecretPickerResponse
{
    public ICollection<SecretModel> Items { get; set; } = [];
    public bool CanCreateInline { get; set; } = true;
}

public class SecretTestResponse
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
}

using Elsa.Common.Entities;

namespace Elsa.Secrets.Models;

public class Secret : Entity
{
    /// <summary>
    /// Assigns the identifier the property initializer used to provide.
    /// </summary>
    /// <remarks>
    /// <see cref="Entity.Id"/> is declared <c>null!</c>, and nothing in this module assigns a secret's id --
    /// there is no identity generator on the create path, so the initializer this replaces was load-bearing.
    /// Dropping it while deriving would have produced a null id on every insert, which unit tests that build
    /// a Secret by hand would not have noticed.
    /// </remarks>
    public Secret()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? Description { get; set; }
    public string TypeName { get; set; } = SecretTypeNames.Text;
    public string StoreName { get; set; } = SecretStoreNames.Encrypted;
    public string? Scope { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(CaseInsensitiveHashSetConverter))]
    public HashSet<string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public SecretStatus Status { get; set; } = SecretStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public IList<SecretVersion> Versions { get; set; } = [];

    public SecretVersion? LatestActiveVersion => Versions
        .Where(x => x.Status == SecretStatus.Active && !x.IsExpired())
        .OrderByDescending(x => x.Version)
        .FirstOrDefault();
}

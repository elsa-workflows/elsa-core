namespace Elsa.Secrets.Management;

public class SecretFilter
{
    public string? Id { get; set; }
    public ICollection<string>? Ids { get; set; }
    public string? SecretId { get; set; }
    public ICollection<string>? SecretIds { get; set; }
    public string? NotSecretId { get; set; }
    public string? Name { get; set; }
    public ICollection<string>? Names { get; set; }
    public int? Version { get; set; }
    public string? Type { get; set; }
    public SecretStatus? Status { get; set; }
    public bool IsLatest { get; set; }
    public string? SearchTerm { get; set; }
    public DateTimeOffset? ExpiresAtLessThan { get; set; }

    public IQueryable<Secret> Apply(IQueryable<Secret> queryable)
    {
        if (Id != null) queryable = queryable.Where(x => x.Id == Id);
        if (Ids != null) queryable = queryable.Where(x => Ids.Contains(x.Id));
        if (SecretId != null) queryable = queryable.Where(x => x.SecretId == SecretId);
        if (SecretIds != null) queryable = queryable.Where(x => SecretIds.Contains(x.SecretId));
        if (NotSecretId != null) queryable = queryable.Where(x => x.SecretId != NotSecretId);
        if (Name != null) queryable = queryable.Where(x => x.Name == Name);
        if (Names != null) queryable = queryable.Where(x => Names.Contains(x.Name));
        if (Version != null) queryable = queryable.Where(x => x.Version == Version);
        if (Type != null) queryable = queryable.Where(x => x.Scope == Type);
        if(Status != null) queryable = queryable.Where(x => x.Status == Status);
        if (IsLatest) queryable = queryable.Where(x => x.IsLatest);
        if (ExpiresAtLessThan != null) queryable = queryable.Where(x => x.ExpiresAt < ExpiresAtLessThan);
        if (!string.IsNullOrWhiteSpace(SearchTerm)) queryable = queryable.Where(x => x.Name.Contains(SearchTerm) || x.Description.Contains(SearchTerm) || x.Id.Contains(SearchTerm));

        return queryable;
    }
}
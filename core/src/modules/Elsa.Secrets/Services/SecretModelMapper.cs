namespace Elsa.Secrets.Services;

public static class SecretModelMapper
{
    public static SecretModel ToModel(this Secret secret)
    {
        var current = secret.LatestActiveVersion;
        return new SecretModel
        {
            Id = secret.Id,
            Name = secret.Name,
            DisplayName = secret.DisplayName,
            Description = secret.Description,
            TypeName = secret.TypeName,
            StoreName = secret.StoreName,
            Scope = secret.Scope,
            Tags = secret.Tags.ToList(),
            Status = ResolveStatus(secret),
            CurrentVersion = current?.Version,
            CreatedAt = secret.CreatedAt,
            UpdatedAt = secret.UpdatedAt,
            ExpiresAt = current?.ExpiresAt
        };
    }

    private static SecretStatus ResolveStatus(Secret secret)
    {
        if (secret.Status != SecretStatus.Active)
            return secret.Status;

        return secret.LatestActiveVersion == null && secret.Versions.Any(x => x.IsExpired()) ? SecretStatus.Expired : secret.Status;
    }
}

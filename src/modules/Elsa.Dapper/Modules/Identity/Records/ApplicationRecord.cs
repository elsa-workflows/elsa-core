namespace Elsa.Dapper.Modules.Identity.Records;

internal class ApplicationRecord
{
    public string Id { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string HashedClientSecret { get; set; } = default!;
    public string HashedClientSecretSalt { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string HashedApiKey { get; set; } = default!;
    public string HashedApiKeySalt { get; set; } = default!;
    public string Roles { get; set; } = default!;
}
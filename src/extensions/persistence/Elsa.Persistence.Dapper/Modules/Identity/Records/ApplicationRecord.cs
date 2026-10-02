using Elsa.Persistence.Dapper.Records;

namespace Elsa.Persistence.Dapper.Modules.Identity.Records;

internal class ApplicationRecord : Record
{
    public string ClientId { get; set; } = null!;
    public string HashedClientSecret { get; set; } = null!;
    public string HashedClientSecretSalt { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string HashedApiKey { get; set; } = null!;
    public string HashedApiKeySalt { get; set; } = null!;
    public string Roles { get; set; } = null!;
}
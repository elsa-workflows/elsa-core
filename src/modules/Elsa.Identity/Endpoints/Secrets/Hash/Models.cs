using System.ComponentModel.DataAnnotations;

namespace Elsa.Identity.Endpoints.Secrets.Hash;

internal class Request
{
    [Required] public string Secret { get; set; } = null!;
}

internal class Response(string hashedSecret, string salt)
{
    public string HashedSecret { get; set; } = hashedSecret;
    public string Salt { get; set; } = salt;
}
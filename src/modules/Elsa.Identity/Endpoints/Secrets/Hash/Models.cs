using System.ComponentModel.DataAnnotations;

namespace Elsa.Identity.Endpoints.Secrets.Hash;

internal class Request
{
    [Required] public string Secret { get; set; } = default!;
}

internal class Response
{
    public Response(string hashedSecret, string salt)
    {
        HashedSecret = hashedSecret;
        Salt = salt;
    }

    public string HashedSecret { get; set; }
    public string Salt { get; set; }
}
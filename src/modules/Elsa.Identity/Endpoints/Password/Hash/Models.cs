namespace Elsa.Identity.Endpoints.Password.Hash;

public class Request
{
    public string Password { get; set; } = default!;
}

public class Response
{
    public Response(string hashedPassword)
    {
        HashedPassword = hashedPassword;
    }

    public string HashedPassword { get; set; }
}
namespace Elsa.Identity.Endpoints.Login;

public class Request
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}
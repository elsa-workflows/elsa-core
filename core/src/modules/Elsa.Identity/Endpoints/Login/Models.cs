namespace Elsa.Identity.Endpoints.Login;

public class Request
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}
namespace Elsa.Identity.Endpoints.Login;

public class Request
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class Response
{
    public Response(bool isAuthenticated, string? accessToken)
    {
        IsAuthenticated = isAuthenticated;
        AccessToken = accessToken;
    }

    public bool IsAuthenticated { get; }
    public string? AccessToken { get;  }
}
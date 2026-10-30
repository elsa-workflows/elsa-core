namespace Elsa.Identity.Endpoints.Logout;

public class Request
{
    /// <summary>
    /// The refresh token of the session to end.
    /// </summary>
    public string RefreshToken { get; set; } = null!;
}

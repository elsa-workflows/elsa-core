namespace Elsa.Tokens.Api.Endpoints.Tokens.Create;

public class Request
{
    public IDictionary<string, string> Credentials { get; set; } = default!;
}

public class Response
{
    public string AccessToken { get; set; } = default!;
}
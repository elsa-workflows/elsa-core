namespace Elsa.Samples.HttpEndpointSecurity.Endpoints.Tokens
{
    public record CreateTokenRequestModel(string UserName, bool IsAdmin);
}
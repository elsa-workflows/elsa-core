namespace Elsa.Samples.HttpEndpointSecurity.Services
{
    public interface ITokenService
    {
        string CreateToken(string userName);
        bool ValidateToken(string token);
    }
}

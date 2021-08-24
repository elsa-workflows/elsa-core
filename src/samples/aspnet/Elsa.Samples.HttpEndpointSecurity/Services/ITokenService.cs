namespace Elsa.Samples.HttpEndpointSecurity.Services
{
    public interface ITokenService
    {
        string CreateToken(string userName, bool isAdmin);
        bool ValidateToken(string token);
    }
}

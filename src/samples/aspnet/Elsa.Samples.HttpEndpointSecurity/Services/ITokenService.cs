namespace Elsa.Samples.HttpEndpointSecurity.Services
{
    public interface ITokenService
    {
        string CreateToken(string userName, bool hasMagic);
        bool ValidateToken(string token);
    }
}

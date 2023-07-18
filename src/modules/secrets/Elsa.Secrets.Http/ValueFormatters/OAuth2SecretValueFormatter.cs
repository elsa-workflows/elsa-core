using System;
using System.Threading.Tasks;
using Elsa.Secrets.Http.Models;
using Elsa.Secrets.Http.Services;
using Elsa.Secrets.Models;
using Elsa.Secrets.Services;
using Elsa.Secrets.ValueFormatters;
using Newtonsoft.Json;

namespace Elsa.Secrets.Http.ValueFormatters
{
    public class OAuth2SecretValueFormatter : ISecretValueFormatter
    {
        private readonly IOAuth2TokenService _tokenService;
        private readonly ISecuredSecretService _securedSecretService;
        public string Type => "OAuth2";

        public OAuth2SecretValueFormatter(IOAuth2TokenService tokenService, ISecuredSecretService securedSecretService)
        {
            _tokenService = tokenService;
            _securedSecretService = securedSecretService;
        }

        public async Task<string> FormatSecretValue(Secret secret)
        {
            _securedSecretService.SetSecret(secret);
            var tokenJson = _securedSecretService.GetProperty("Token") ?? string.Empty;
            var tokenData =  JsonConvert.DeserializeObject<TokenData>(tokenJson);
            
            if (_securedSecretService.GetProperty("GrantType") == "authorization_code" && tokenData == null)
                throw new Exception("OAuth2 refresh token has expired - credential must be authorized with OAuth2 provider");

            if (tokenData?.ExpiresAtUtc > DateTime.UtcNow)
                return $"{tokenData.TokenType ?? "Bearer"} {tokenData.AccessToken}";

            TokenResponse response;
            if (!string.IsNullOrEmpty(tokenData?.RefreshToken))
            {
                response = await _tokenService.GetTokenByRefreshToken(secret, tokenData.RefreshToken);
            }
            else
            {
                response = await _tokenService.GetToken(secret);
            }
            
            return $"{response.TokenType ?? "Bearer"} {response.AccessToken}";
        }
    }
}
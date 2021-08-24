using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Elsa.Samples.HttpEndpointSecurity.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Elsa.Samples.HttpEndpointSecurity.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtOptions _options;

        public TokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public string CreateToken(string userName, bool isAdmin)
        {
            var claims = new List<Claim>()
            {
                new(JwtRegisteredClaimNames.Sub, userName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (isAdmin)
                claims.Add(new Claim("is-admin", "true"));

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var tokenDescriptor = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: DateTime.Now.AddYears(1), signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        public bool ValidateToken(string token)
        {
            var mySecret = Encoding.UTF8.GetBytes(_options.SecretKey);
            var mySecurityKey = new SymmetricSecurityKey(mySecret);

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _options.Issuer,
                    ValidAudience = _options.Audience,
                    IssuerSigningKey = mySecurityKey,
                }, out SecurityToken validatedToken);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
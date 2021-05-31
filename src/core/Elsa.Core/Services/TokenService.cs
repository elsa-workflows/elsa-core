using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace Elsa.Services
{
    public class TokenService : ITokenService
    {
        private readonly IDataProtector _dataProtector;

        public TokenService(IDataProtectionProvider dataProtectionProvider)
        {
            _dataProtector = dataProtectionProvider.CreateProtector("Elsa Tokens");
        }

        public string CreateToken<T>(T payload)
        {
            var json = JsonConvert.SerializeObject(payload);

            return _dataProtector.Protect(json);
        }

        public bool TryDecryptToken<T>(string token, out T payload)
        {
            payload = default!;

            try
            {
                var json = _dataProtector.Unprotect(token);
                payload = JsonConvert.DeserializeObject<T>(json)!;
                return true;
            }
            catch
            {
                // ignored.
            }

            return false;
        }

        public T DecryptToken<T>(string token)
        {
            var json = _dataProtector.Unprotect(token);
            return JsonConvert.DeserializeObject<T>(json)!;
        }
    }
}
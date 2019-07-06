using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace Elsa.Activities.Http.Services
{
    public class SharedAccessSignatureService : ISharedAccessSignatureService
    {
        private readonly IDataProtector dataProtector;

        public SharedAccessSignatureService(IDataProtectionProvider dataProtectionProvider)
        {
            dataProtector = dataProtectionProvider.CreateProtector("HTTP Workflow Tokens");
        }
        
        public string CreateToken<T>(T payload)
        {
            var json = JsonConvert.SerializeObject(payload);

            return dataProtector.Protect(json);
        }

        public bool TryDecryptToken<T>(string token, out T payload)
        {
            payload = default;

            try
            {
                var json = dataProtector.Unprotect(token);

                payload = JsonConvert.DeserializeObject<T>(json);
                return true;
            }
            catch
            {
                // ignored.
            }

            return false;

        }
    }
}
using Elsa.Secrets.Models;

namespace Elsa.Secrets.Events
{
    public class SecretSaved : SecretsNotification
    {
        public SecretSaved(Secret secret) : base(secret)
        {
        }
    }
}

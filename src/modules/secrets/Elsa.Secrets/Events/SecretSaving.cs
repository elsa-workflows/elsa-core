using Elsa.Secrets.Models;

namespace Elsa.Secrets.Events
{
    public class SecretSaving : SecretsNotification
    {
        public SecretSaving(Secret secret) : base(secret)
        {
        }
    }
}

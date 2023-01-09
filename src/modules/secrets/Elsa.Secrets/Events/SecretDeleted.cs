using Elsa.Secrets.Models;

namespace Elsa.Secrets.Events
{
    public class SecretDeleted : SecretsNotification
    {
        public SecretDeleted(Secret secret) : base(secret)
        {
        }
    }
}

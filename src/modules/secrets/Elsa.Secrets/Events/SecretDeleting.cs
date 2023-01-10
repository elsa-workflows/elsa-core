using Elsa.Secrets.Models;

namespace Elsa.Secrets.Events
{
    public class SecretDeleting : SecretsNotification
    {
        public SecretDeleting(Secret secret) : base(secret)
        { 
        }
    }
}

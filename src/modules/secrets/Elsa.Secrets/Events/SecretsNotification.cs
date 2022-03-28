using Elsa.Secrets.Models;
using MediatR;

namespace Elsa.Secrets.Events
{
    public abstract class SecretsNotification : INotification
    {
        public SecretsNotification(Secret secret) => Secret = secret;
        public Secret Secret { get; }
    }
}

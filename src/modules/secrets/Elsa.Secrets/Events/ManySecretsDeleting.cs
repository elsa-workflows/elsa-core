using Elsa.Secrets.Models;
using MediatR;
using System.Collections.Generic;

namespace Elsa.Secrets.Events
{
    public class ManySecretsDeleting : INotification
    {
        public ManySecretsDeleting(IEnumerable<Secret> secrets) => Secrets = secrets;
        public IEnumerable<Secret> Secrets { get; }
    }
}

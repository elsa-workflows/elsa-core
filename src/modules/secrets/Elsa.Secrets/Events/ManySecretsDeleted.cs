using Elsa.Secrets.Models;
using MediatR;
using System.Collections.Generic;

namespace Elsa.Secrets.Events
{
    public class ManySecretsDeleted : INotification
    {
        public ManySecretsDeleted(IEnumerable<Secret> secrets) => Secrets = secrets;
        public IEnumerable<Secret> Secrets { get; }
    }
}

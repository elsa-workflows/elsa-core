using Elsa.Secrets.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Events
{
    public class ManySecretsDeleting : INotification
    {
        public ManySecretsDeleting(IEnumerable<Secret> secrets) => Secrets = secrets;
        public IEnumerable<Secret> Secrets { get; }
    }
}

using Elsa.Secrets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Events
{
    public class SecretDeleted : SecretsNotification
    {
        public SecretDeleted(Secret secret) : base(secret)
        {
        }
    }
}

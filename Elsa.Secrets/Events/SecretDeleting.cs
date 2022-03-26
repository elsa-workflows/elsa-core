using Elsa.Secrets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Events
{
    public class SecretDeleting : SecretsNotification
    {
        public SecretDeleting(Secret secret) : base(secret)
        { 
        }
    }
}

using Elsa.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Models
{
    public class Secret : Entity
    {
        public Secret()
        {
            Properties = new List<SecretProperty>();
        }
        public string Type { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public ICollection<SecretProperty> Properties { get; set; }
    }
}

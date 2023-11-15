using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.Models
{
    public  class OptionsProviderResult
    {
        public IDictionary<string, object> ProviderMetadata { get; set; } = new Dictionary<string, object>();

        public IDictionary<string, object> OptionsItems { get; set; }
    }
}

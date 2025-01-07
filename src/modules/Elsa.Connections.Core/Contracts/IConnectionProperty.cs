using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Elsa.Connections.Contracts;
interface IConnectionProperty
{
    public string ConnectionName { get; set; }

    [JsonIgnore]
    public object Properties { get; set; }
}


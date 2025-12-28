using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Workflows.Management.Options;

public sealed class PayloadPersistenceOption
{
    public PayloadPersistenceOption()
    {
        
    }

    public PayloadPersistenceOption(string payloadTypeIdentifier, PayloadPersistenceMode mode)
    {
        PayloadTypeIdentifier = payloadTypeIdentifier;
        Mode = mode;
    }

    public string PayloadTypeIdentifier { get; set; } = string.Empty;

    public PayloadPersistenceMode Mode { get; set; }
}

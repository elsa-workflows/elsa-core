using System;
using System.Collections.Generic;
using Elsa.Activities.Telnyx.Payloads;

namespace Elsa.Activities.Telnyx.Options
{
    public class TelnyxOptions
    {
        public TelnyxOptions()
        {
            PayloadTypes = new List<Type>
            {
                typeof(CallInitiatedPayload)
            };
        }
        
        public IList<Type> PayloadTypes { get; set; }
    }
}
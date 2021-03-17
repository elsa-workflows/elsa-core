using System;
using System.Collections.Generic;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;

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
        public Uri ApiUrl { get; set; } = default!;
        public string ApiKey { get; set; } = default!;
    }
}
using System;
using System.Collections.Generic;
using Elsa.Activities.Telnyx.Services;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Microsoft.Extensions.DependencyInjection;

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

            ExtensionProviderFactory = sp => ActivatorUtilities.CreateInstance<NullExtensionProvider>(sp);
        }
        
        public IList<Type> PayloadTypes { get; set; }
        public Uri ApiUrl { get; set; } = new("https://api.telnyx.com");
        public string ApiKey { get; set; } = default!;
        public string? CallControlAppId { get; set; }
        internal Func<IServiceProvider, IExtensionProvider> ExtensionProviderFactory { get; set; }

        public TelnyxOptions UseExtensionProvider(Func<IServiceProvider, IExtensionProvider> factory)
        {
            ExtensionProviderFactory = factory;
            return this;
        }
    }
}
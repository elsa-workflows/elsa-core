using System;
using Elsa.Activities.Http;
using Elsa.Secrets.Providers;

namespace Elsa.Secrets.Enrichers
{
    public class SendHttpRequestAuthorizationInputDescriptorEnricher : BaseActivityInputDescriptorEnricher
    {
        public SendHttpRequestAuthorizationInputDescriptorEnricher(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public override Type ActivityType => typeof(SendHttpRequest);
        public override string PropertyName => nameof(SendHttpRequest.Authorization);

        public override Type OptionsProvider => typeof(SecretAuthorizationHeaderOptionsProvider);
    }
}

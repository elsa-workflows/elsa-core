using Elsa.Activities.Http.Activities;
using Elsa.Handlers;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Http.Descriptors
{
    public class HttpRequestTriggerDescriptor : ActivityDescriptorBase<HttpRequestTrigger>
    {
        public HttpRequestTriggerDescriptor(IStringLocalizer<HttpRequestTrigger> localizer)
        {
            T = localizer;
        }

        public IStringLocalizer<HttpRequestTrigger> T { get; }
        public override bool IsTrigger => true;
        public override LocalizedString Category => T["HTTP"];
        public override LocalizedString DisplayText => T["HTTP Request"];
        public override LocalizedString Description => T["Triggers when an incoming HTTP request is received."];
        protected override LocalizedString GetEndpoint() => T["Done"];
    }
}
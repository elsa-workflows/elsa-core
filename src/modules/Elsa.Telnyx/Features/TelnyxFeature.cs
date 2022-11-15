using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Options;
using Elsa.Telnyx.Providers;
using Elsa.Workflows.Management.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Telnyx.Features;

public class TelnyxFeature : FeatureBase
{
    public TelnyxFeature(IModule module) : base(module)
    {
    }

    public Action<TelnyxOptions> ConfigureTelnyxOptions { get; set; } = _ => { };
    public Func<IServiceProvider, HttpClient>? HttpClientFactory { get; set; }
    public Action<IHttpClientBuilder>? ConfigureHttpClientBuilder { get; set; }

    public override void Configure()
    {
        Module.UseWorkflowManagement(management => management.AddActivity<IncomingCall>());
    }

    public override void Apply()
    {
        Services
            .AddTelnyx(ConfigureTelnyxOptions, HttpClientFactory, ConfigureHttpClientBuilder)
            .AddActivityProvider<WebhookEventActivityProvider>();
    }
}
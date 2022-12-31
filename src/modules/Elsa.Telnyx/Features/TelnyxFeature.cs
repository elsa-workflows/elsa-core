using System.ComponentModel;
using System.Reflection;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Options;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Telnyx.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Telnyx.Features;

/// <summary>
/// Enables Telnyx integration.
/// </summary>
public class TelnyxFeature : FeatureBase
{
    private const string TelnyxCategoryName = "Telnyx";

    public TelnyxFeature(IModule module) : base(module)
    {
    }

    public Action<TelnyxOptions> ConfigureTelnyxOptions { get; set; } = _ => { };
    public Func<IServiceProvider, HttpClient>? HttpClientFactory { get; set; }
    public Action<IHttpClientBuilder>? ConfigureHttpClientBuilder { get; set; }

    public override void Configure()
    {
        Module.UseWorkflowManagement(management =>
        {
            management.AddActivitiesFrom<IncomingCall>();
            
            management.AddVariableTypes(typeof(TelnyxFeature).Assembly.ExportedTypes.Where(x =>
            {
                var browsableAttr = x.GetCustomAttribute<BrowsableAttribute>();
                return typeof(Payload).IsAssignableFrom(x) && browsableAttr == null || browsableAttr?.Browsable == true;
            }), TelnyxCategoryName);

            management.AddVariableType<DialResponse>(TelnyxCategoryName);
            management.AddVariableType<NumberLookupResponse>(TelnyxCategoryName);
            management.AddVariableType<Carrier>(TelnyxCategoryName);
            management.AddVariableType<CallerName>(TelnyxCategoryName);
        });
    }

    public override void Apply()
    {
        Services
            .AddTelnyx(ConfigureTelnyxOptions, HttpClientFactory, ConfigureHttpClientBuilder)
            .AddActivityProvider<WebhookEventActivityProvider>();
    }
}
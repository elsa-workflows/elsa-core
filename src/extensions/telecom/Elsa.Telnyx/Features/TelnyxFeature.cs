using System.ComponentModel;
using System.Reflection;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Options;
using Elsa.Telnyx.Payloads.Abstractions;
using Elsa.Telnyx.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Telnyx.Features;

/// <summary>
/// Enables Telnyx integration.
/// </summary>
public class TelnyxFeature : FeatureBase
{
    private const string TelnyxCategoryName = "Telnyx";

    /// <inheritdoc />
    public TelnyxFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Configures Telnyx options.
    /// </summary>
    public Action<TelnyxOptions> ConfigureTelnyxOptions { get; set; } = _ => { };
    
    /// <summary>
    /// Gets or sets a factory that creates an <see cref="HttpClient"/> used to communicate with the Telnyx API.
    /// </summary>
    public Func<IServiceProvider, HttpClient>? HttpClientFactory { get; set; }
    
    /// <summary>
    /// Configures the <see cref="IHttpClientBuilder"/> used to communicate with the Telnyx API.
    /// </summary>
    public Action<IHttpClientBuilder>? ConfigureHttpClientBuilder { get; set; }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddTelnyx(ConfigureTelnyxOptions, HttpClientFactory, ConfigureHttpClientBuilder)
            .AddActivityProvider<WebhookEventActivityProvider>();
    }
}
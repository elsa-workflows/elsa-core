using Elsa.Email.Contracts;
using Elsa.Email.Options;
using Elsa.Email.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Email.Features;

/// <summary>
/// Setup email features.
/// </summary>
public class EmailFeature : FeatureBase
{
    /// <inheritdoc />
    public EmailFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Set a callback to configure <see cref="SmtpOptions"/>.
    /// </summary>
    public Action<SmtpOptions> ConfigureOptions { get; set; } = _ => { };

    /// <summary>
    /// Set a callback for configuring the HTTP client use by the default implementation of <see cref="IDownloader"/>.
    /// </summary>
    public Action<IServiceProvider, HttpClient> ConfigureDownloaderHttpClient { get; set; } = (_, _) => { };

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddActivitiesFrom<EmailFeature>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .Configure(ConfigureOptions)
            .AddSingleton<ISmtpService, MailKitSmtpService>()
            .AddHttpClient<IDownloader, DefaultDownloader>(ConfigureDownloaderHttpClient);
    }
}
using CShells.Features;
using Elsa.Email.Contracts;
using Elsa.Email.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Email.ShellFeatures;

/// <summary>
/// Shell feature for email functionality.
/// </summary>
[ShellFeature(
    DisplayName = "Email",
    Description = "Provides email activities and SMTP service support for workflows")]
[UsedImplicitly]
public class EmailShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<MailKitSmtpService>();
        services.AddScoped<ISmtpService, MailKitSmtpService>();
        services.AddHttpClient<IDownloader, DefaultDownloader>();
    }
}


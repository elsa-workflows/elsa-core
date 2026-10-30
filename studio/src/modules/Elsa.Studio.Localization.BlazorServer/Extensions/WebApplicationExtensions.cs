using Elsa.Studio.Localization.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Localization.BlazorServer.Extensions;

/// <summary>
/// Provides extension methods for web application.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the elsa localization.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The result of the operation.</returns>
    public static WebApplication UseElsaLocalization(this WebApplication app)
    {
        var localisationOptions = app.Services.GetService<IOptions<LocalizationOptions>>()?.Value;
        var defaultCulture = localisationOptions?.DefaultCulture;
        var supportedCultures = localisationOptions?.SupportedCultures;

        if (supportedCultures != null)
        {
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture(defaultCulture ?? new LocalizationOptions().DefaultCulture)
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            app.UseRequestLocalization(localizationOptions);
        }

        return app;
    }
}
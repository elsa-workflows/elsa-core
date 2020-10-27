using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Elsa.Activities.Dropbox.Activities;
using Elsa.Activities.Dropbox.Options;
using Elsa.Activities.Dropbox.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Dropbox.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDropboxActivities(
            this IServiceCollection services,
            Action<OptionsBuilder<DropboxOptions>> options = null)
        {
            var optionsBuilder = services.AddOptions<DropboxOptions>();
            options?.Invoke(optionsBuilder);

            services
                .AddHttpClient<IFilesApi, FilesApi>()
                .ConfigureHttpClient(ConfigureHttpClient);

            return services
                .AddActivity<SaveToDropbox>();
        }

        private static void ConfigureHttpClient(IServiceProvider services, HttpClient httpClient)
        {
            var options = services.GetRequiredService<IOptions<DropboxOptions>>().Value;

            httpClient.BaseAddress =
                options.ContentServiceUrl ?? new Uri(
                    "https://content.dropboxapi.com",
                    UriKind.Absolute
                );

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.AccessToken);
        }
    }
}
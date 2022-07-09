using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Elsa.Activities.Dropbox.Activities;
using Elsa.Activities.Dropbox.Options;
using Elsa.Activities.Dropbox.Services;
using Elsa.Options;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddDropbox(this ElsaOptionsBuilder services, Action<DropboxOptions>? configureOptions = null) =>
            services
                .AddDropboxServices(configureOptions)
                .AddDropboxActivities();

        public static ElsaOptionsBuilder AddDropboxServices(this ElsaOptionsBuilder options, Action<DropboxOptions>? configureOptions = null)
        {
            if (configureOptions != null) 
                options.Services.Configure(configureOptions);

            options.Services
                .AddHttpClient<IFilesApi, FilesApi>()
                .ConfigureHttpClient(ConfigureHttpClient);

            return options;
        }
        
        public static ElsaOptionsBuilder AddDropboxActivities(this ElsaOptionsBuilder services) => services.AddActivity<SaveToDropbox>();

        private static void ConfigureHttpClient(IServiceProvider services, HttpClient httpClient)
        {
            var options = services.GetRequiredService<IOptions<DropboxOptions>>().Value;

            httpClient.BaseAddress =
                options.ContentServiceUrl ?? new Uri(
                    "https://content.dropboxapi.com",
                    UriKind.Absolute
                );

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        }
    }
}
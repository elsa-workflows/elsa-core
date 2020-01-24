using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Elsa.Activities.Dropbox.Activities;
using Elsa.Activities.Dropbox.Options;
using Elsa.Activities.Dropbox.Services;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDropbox(this IServiceCollection services, Action<OptionsBuilder<DropboxOptions>>? options = null) =>
            services
                .AddDropboxServices(options)
                .AddActivity<SaveToDropbox>();

        public static IServiceCollection AddDropboxServices(this IServiceCollection services, Action<OptionsBuilder<DropboxOptions>>? options = null)
        {
            var optionsBuilder = services.AddOptions<DropboxOptions>();
            options?.Invoke(optionsBuilder);

            services
                .AddHttpClient<IFilesApi, FilesApi>()
                .ConfigureHttpClient(ConfigureHttpClient);

            return services;
        }
        
        public static IServiceCollection AddDropboxActivities(this IServiceCollection services) => services.AddActivity<SaveToDropbox>();

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
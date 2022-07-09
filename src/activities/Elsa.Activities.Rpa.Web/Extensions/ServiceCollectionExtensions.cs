using Elsa.Activities.Rpa.Web;
using Elsa.Activities.Rpa.Web.Options;
using Elsa.Activities.Rpa.Web.Services;
using System;
using Elsa.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddRpaWebActivities(this ElsaOptionsBuilder options, Action<RpaWebOptions>? configureOptions = null)
        {
            options.Services.AddRpaWebServices(configureOptions);
            options.AddRpaWebActivitiesInternal();
            return options;
        }

        public static IServiceCollection AddRpaWebServices(this IServiceCollection services, Action<RpaWebOptions>? configureOptions = null)
        {
            if (configureOptions != null) 
                services.Configure(configureOptions);

            return services.AddSingleton<IBrowserFactory, BrowserFactory>();
        }

        private static ElsaOptionsBuilder AddRpaWebActivitiesInternal(this ElsaOptionsBuilder services) => services
            .AddActivity<OpenBrowser>()
            .AddActivity<CloseBrowser>()
            .AddActivity<NavigateToUrl>()
            .AddActivity<ClickElement>()
            .AddActivity<TypeText>()
            .AddActivity<GetText>()
            ;
    }
}
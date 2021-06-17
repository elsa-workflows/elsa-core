using System;
using Elsa;
using Elsa.Activities.Email;
using Elsa.Activities.Email.Handlers;
using Elsa.Activities.Email.Options;
using Elsa.Activities.Email.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddEmailActivities(this ElsaOptionsBuilder options, Action<SmtpOptions>? configureOptions = null)
        {
            options.Services.AddEmailServices(configureOptions);
            options.Services.AddNotificationHandlersFrom<ConfigureJavaScriptEngine>();
            options.Services.AddJavaScriptTypeDefinitionProvider<EmailTypeDefinitionProvider>();
            options.Services.AddHttpClient();
            options.AddEmailActivitiesInternal();
            return options;
        }

        public static IServiceCollection AddEmailServices(this IServiceCollection services, Action<SmtpOptions>? configureOptions = null)
        {
            if (configureOptions != null) 
                services.Configure(configureOptions);

            return services.AddSingleton<ISmtpService, MailKitSmtpService>();
        }

        private static ElsaOptionsBuilder AddEmailActivitiesInternal(this ElsaOptionsBuilder services) => services.AddActivity<SendEmail>();
    }
}
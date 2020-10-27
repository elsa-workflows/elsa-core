using System;
using Elsa.Activities.Email;
using Elsa.Activities.Email.Options;
using Elsa.Activities.Email.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEmailActivities(this IServiceCollection services, Action<SmtpOptions> configureOptions = null) =>
            services
                .AddEmailServices(configureOptions)
                .AddEmailActivitiesInternal();
        
        public static IServiceCollection AddEmailServices(this IServiceCollection services, Action<SmtpOptions> configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services
                .AddSingleton<ISmtpService, SmtpService>();
        }

        private static IServiceCollection AddEmailActivitiesInternal(this IServiceCollection services) => services.AddActivity<SendEmail>();
    }
}
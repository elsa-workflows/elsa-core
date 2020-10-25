using System;
using Elsa.Activities.Email;
using Elsa.Activities.Email.Options;
using Elsa.Activities.Email.Services;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEmailActivities(this IServiceCollection services, Action<OptionsBuilder<SmtpOptions>> options = null) =>
            services
                .AddEmailServices(options)
                .AddEmailActivitiesInternal();
        
        public static IServiceCollection AddEmailServices(this IServiceCollection services, Action<OptionsBuilder<SmtpOptions>> options = null)
        {
            var optionsBuilder = services.AddOptions<SmtpOptions>();
            options?.Invoke(optionsBuilder);

            return services
                .AddSingleton<ISmtpService, SmtpService>();
        }

        private static IServiceCollection AddEmailActivitiesInternal(this IServiceCollection services) => services.AddActivity<SendEmail>();
    }
}
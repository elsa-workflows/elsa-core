using System;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Email.Options;
using Elsa.Activities.Email.Services;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEmail(this IServiceCollection services, Action<OptionsBuilder<SmtpOptions>> options = null) =>
            services
                .AddEmailServices(options)
                .AddEmailActivities();
        
        public static IServiceCollection AddEmailServices(this IServiceCollection services, Action<OptionsBuilder<SmtpOptions>> options = null)
        {
            var optionsBuilder = services.AddOptions<SmtpOptions>();
            options?.Invoke(optionsBuilder);

            return services
                .AddOptions()
                .AddSingleton<ISmtpService, SmtpService>();
        }
        
        public static IServiceCollection AddEmailActivities(this IServiceCollection services) => services.AddActivity<SendEmail>();
    }
}
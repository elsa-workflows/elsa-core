using System;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Email.Options;
using Elsa.Activities.Email.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Email.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEmailActivities(this IServiceCollection services, Action<OptionsBuilder<SmtpOptions>> options = null)
        {
            var optionsBuilder = services.AddOptions<SmtpOptions>();
            options?.Invoke(optionsBuilder);
            
            return services
                .AddOptions()
                .AddSingleton<ISmtpService, SmtpService>()
                .AddActivity<SendEmail>();
        }
    }
}
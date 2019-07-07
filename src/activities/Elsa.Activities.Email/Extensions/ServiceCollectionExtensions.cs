using System;
using System.Net.Mail;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Email.Options;
using Elsa.Core.Extensions;
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
                .AddSingleton(CreateSmtpClient)
                .AddActivity<SendEmail>();
        }

        private static SmtpClient CreateSmtpClient(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<SmtpOptions>>().Value;
            
            return new SmtpClient(options.Host, options.Port);
        }
    }
}
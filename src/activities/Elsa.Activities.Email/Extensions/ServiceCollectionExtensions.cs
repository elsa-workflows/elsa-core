using System;
using System.Net;
using System.Net.Mail;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Email.Options;
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
            var smtpClient = new SmtpClient(options.Host, options.Port);
            var credentials = options.Credentials;
            
            if (credentials != null && !string.IsNullOrWhiteSpace(credentials.Username)) 
                smtpClient.Credentials = new NetworkCredential(credentials.Username, credentials.Password);

            if (options.Timeout != null)
                smtpClient.Timeout = (int)options.Timeout.Value.TotalSeconds;

            if (options.DeliveryFormat != null)
                smtpClient.DeliveryFormat = options.DeliveryFormat.Value;

            if (options.DeliveryMethod != null)
                smtpClient.DeliveryMethod = options.DeliveryMethod.Value;

            if (options.EnableSsl != null)
                smtpClient.EnableSsl = options.EnableSsl.Value;

            if (!string.IsNullOrWhiteSpace(options.PickupDirectoryLocation))
                smtpClient.PickupDirectoryLocation = options.PickupDirectoryLocation;

            return smtpClient;
        }
    }
}
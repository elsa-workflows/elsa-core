using System;
using System.Net.Mail;
using Elsa.Activities.Email.Drivers;
using Elsa.Activities.Email.Options;
using Elsa.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Email.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEmailActivities(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .AddOptions()
                .Configure<SmtpOptions>(configuration)
                .AddSingleton(CreateSmtpClient)
                .AddActivity<SendEmailDriver>();
        }

        private static SmtpClient CreateSmtpClient(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<SmtpOptions>>().Value;
            
            return new SmtpClient(options.Host, options.Port);
        }
    }
}
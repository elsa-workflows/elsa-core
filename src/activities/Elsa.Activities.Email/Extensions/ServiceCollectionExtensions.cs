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
        public static IServiceCollection AddEmailDescriptors(this IServiceCollection services)
        {
            return services.AddActivityDescriptors<ActivityDescriptors>();
        }

        public static IServiceCollection AddEmailActivities(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddEmailDescriptors();
            
            services
                .AddOptions()
                .Configure<SmtpOptions>(configuration)
                .AddSingleton(CreateSmtpClient)
                .AddActivityDriver<SendEmailDriver>();
            
            return services;
        }

        private static SmtpClient CreateSmtpClient(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<SmtpOptions>>().Value;
            
            return new SmtpClient(options.Host, options.Port);
        }
    }
}
using MediatR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Elsa.Extensions;
using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Options;
using Elsa.AzureServiceBus.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AzureServiceBusServiceCollectionExtensions
    {
        public static IServiceCollection AddAzureServiceBusActivities(this IServiceCollection services, Action<OptionsBuilder<ServiceBusOptions>> options = null)
        {
            var optionsBuilder = services.AddOptions<ServiceBusOptions>();
            options?.Invoke(optionsBuilder);

            return services.AddOptions()                
                .AddScoped<IServiceBusClientFactory, ServiceBusClientFactory>()
                .AddActivity<ServiceBusSend>()
                .AddActivity<ServiceBusSignaled>()
                .AddActivity<ServiceBusSignalReceived>()
                .AddTransient<IMessageHandlerMediatorService, MessageHandlerMediatorService>()
                .AddSingleton<ITokenService, TokenService>()
                .AddSingleton<IServiceBusConsumer, ServiceBusConsumer>()
                .AddNotificationHandlers(typeof(AzureServiceBusServiceCollectionExtensions))
            ;
            //.AddActivity<MBSActivities.Orders.SalesConfirmation>();
        }
    }
}

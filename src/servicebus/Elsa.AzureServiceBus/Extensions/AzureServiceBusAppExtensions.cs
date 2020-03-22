using Elsa.AzureServiceBus.Services;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AzureServiceBusAppExtensions
    {
        public static IApplicationBuilder UseAzureServiceBusMessageConsumer(this IApplicationBuilder app)
        {
            var bus = app.ApplicationServices.GetService<IServiceBusConsumer>();
            bus.RegisterOnMessageHandlerAndReceiveMessages();

            return app;
        }
    }
}

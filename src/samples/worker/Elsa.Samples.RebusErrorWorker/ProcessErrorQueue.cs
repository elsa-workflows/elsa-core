using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Routing.TransportMessages;
using Rebus.Serialization.Json;
using Rebus.ServiceProvider;

namespace Elsa.Samples.RebusErrorWorker;

public class ProcessErrorQueue : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public ProcessErrorQueue(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var handlerActivator = new DependencyInjectionHandlerActivator(_serviceProvider);
        var configurer = Configure.With(handlerActivator);

        configurer
            .Serialization(serializer => serializer.UseNewtonsoftJson(DefaultContentSerializer.CreateDefaultJsonSerializationSettings()))
            .Transport(transport => transport.UseAzureServiceBus(_configuration.GetConnectionString("AzureServiceBus"), "error"))
            .Routing(r =>
            {
                r.AddTransportMessageForwarder(transportMessage =>
                {
                    var returnAddress = transportMessage.Headers[Headers.SourceQueue];

                    if (returnAddress.Contains("workflow-management-events"))
                        return Task.FromResult(ForwardAction.Ignore());

                    return Task.FromResult(ForwardAction.ForwardTo(returnAddress));
                });
            });

        var bus = configurer.Start();

        return Task.CompletedTask;
    }
}
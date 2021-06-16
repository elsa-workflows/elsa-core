using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Serialization;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Serialization.Json;
using Rebus.ServiceProvider;

namespace Elsa.Services.Messaging
{
    public class ServiceBusFactory : IServiceBusFactory
    {
        private readonly ElsaOptions _elsaOptions;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, IBus> _serviceBuses = new();
        private readonly DependencyInjectionHandlerActivator _handlerActivator;
        private readonly SemaphoreSlim _semaphore = new(1);

        public ServiceBusFactory(ElsaOptions elsaOptions, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            _elsaOptions = elsaOptions;
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
            _handlerActivator = new DependencyInjectionHandlerActivator(serviceProvider);
        }

        public Task<IBus> GetServiceBusAsync(Type messageType, CancellationToken cancellationToken = default) => GetServiceBusAsync(messageType, messageType.Name, cancellationToken);

        public async Task<IBus> GetServiceBusAsync(Type messageType, string queueName, CancellationToken cancellationToken = default)
        {
            var formattedQueueName = FormatQueueName(queueName);
            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                if (_serviceBuses.TryGetValue(formattedQueueName, out var bus))
                    return bus;

                var configurer = Configure.With(_handlerActivator);
                var map = new Dictionary<Type, string> { [messageType] = formattedQueueName };
                var configureContext = new ServiceBusEndpointConfigurationContext(configurer, formattedQueueName, map, _serviceProvider);

                // Default options.
                configurer
                    .Serialization(serializer => serializer.UseNewtonsoftJson(DefaultContentSerializer.CreateDefaultJsonSerializationSettings()))
                    .Logging(l => l.MicrosoftExtensionsLogging(_loggerFactory))
                    .Routing(r => r.TypeBased().Map(map))
                    .Options(options => options.Apply(_elsaOptions.ServiceBusOptions));
                
                // Configure transport.
                _elsaOptions.ConfigureServiceBusEndpoint(configureContext);
            
                var newBus = configurer.Start();
                _serviceBuses.TryAdd(formattedQueueName, newBus);

                return newBus;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private string FormatQueueName(string name) => $"{_elsaOptions.ServiceBusOptions.QueuePrefix}{name}";
    }
}
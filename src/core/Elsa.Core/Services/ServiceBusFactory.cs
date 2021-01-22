using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Config;
using Rebus.ServiceProvider;

namespace Elsa.Services
{
    public class ServiceBusFactory : IServiceBusFactory
    {
        private readonly ElsaOptions _elsaOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, IBus> _serviceBuses = new();
        private readonly DependencyInjectionHandlerActivator _handlerActivator;
        private readonly SemaphoreSlim _semaphore = new(1);

        public ServiceBusFactory(ElsaOptions elsaOptions, IServiceProvider serviceProvider)
        {
            _elsaOptions = elsaOptions;
            _serviceProvider = serviceProvider;
            _handlerActivator = new DependencyInjectionHandlerActivator(serviceProvider);
        }

        public async Task<IBus> GetServiceBusAsync(Type messageType, CancellationToken cancellationToken)
        {
            var queueName = FormatQueueName(messageType.Name);
            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                if (_serviceBuses.TryGetValue(queueName, out var bus))
                    return bus;

                var configurer = Configure.With(_handlerActivator);
                var map = new Dictionary<Type, string> { [messageType] = queueName };
                var configureContext = new ServiceBusEndpointConfigurationContext(configurer, queueName, map, _serviceProvider);

                _elsaOptions.ConfigureServiceBusEndpoint(configureContext);
            
                var newBus = configurer.Start();
                _serviceBuses.TryAdd(queueName, newBus);

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
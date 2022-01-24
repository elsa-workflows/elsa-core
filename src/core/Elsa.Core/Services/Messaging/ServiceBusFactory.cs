using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elsa.Options;
using Elsa.Serialization;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Serialization.Json;
using Rebus.ServiceProvider;

namespace Elsa.Services.Messaging
{
    public class ServiceBusFactory : IServiceBusFactory, IDisposable
    {
        private readonly ElsaOptions _elsaOptions;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<string, IBus> _serviceBuses = new Dictionary<string, IBus>();
        private readonly IDictionary<Type, string> _messageTypeQueueDictionary = new Dictionary<Type, string>();
        private readonly DependencyInjectionHandlerActivator _handlerActivator;
        private readonly SemaphoreSlim _semaphore = new(1);

        public ServiceBusFactory(ElsaOptions elsaOptions, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            _elsaOptions = elsaOptions;
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
            _handlerActivator = new DependencyInjectionHandlerActivator(serviceProvider);
        }

        public void Dispose()
        {
            foreach (var bus in _serviceBuses.Values)
                bus.Dispose();
        }

        public IBus ConfigureServiceBus(IEnumerable<Type> messageTypes, string queueName)
        {
            queueName = ServiceBusOptions.FormatQueueName(queueName);
            var prefixedQueueName = PrefixQueueName(queueName);
            var messageTypeList = messageTypes.ToList();
            var configurer = Configure.With(_handlerActivator);
            var map = messageTypeList.ToDictionary(x => x, _ => prefixedQueueName);
            var configureContext = new ServiceBusEndpointConfigurationContext(configurer, prefixedQueueName, map, _serviceProvider);

            // Default options.
            configurer
                .Serialization(serializer => serializer.UseNewtonsoftJson(DefaultContentSerializer.CreateDefaultJsonSerializationSettings()))
                .Logging(l => l.MicrosoftExtensionsLogging(_loggerFactory))
                .Routing(r => r.TypeBased().Map(map))
                .Options(options => options.Apply(_elsaOptions.ServiceBusOptions));

            // Configure transport.
            _elsaOptions.ConfigureServiceBusEndpoint(configureContext);

            var newBus = configurer.Start();
            _serviceBuses.Add(prefixedQueueName, newBus);

            foreach (var messageType in messageTypeList)
                _messageTypeQueueDictionary[messageType] = prefixedQueueName;

            return newBus;
        }

        public IBus GetServiceBus(Type messageType, string? queueName = default) => GetOrCreateServiceBus(messageType, queueName);

        private IBus GetOrCreateServiceBus(Type messageType, string? queueName)
        {
            _semaphore.Wait();

            try
            {
                if (queueName == null)
                {
                    if (_messageTypeQueueDictionary.ContainsKey(messageType))
                        queueName = _messageTypeQueueDictionary[messageType];
                    else
                        queueName = PrefixQueueName(ServiceBusOptions.FormatQueueName(messageType.Name));
                }

                if (!_serviceBuses.TryGetValue(queueName, out var bus))
                {
                    bus = ConfigureServiceBus(new[] { messageType }, queueName);
                    _serviceBuses[queueName] = bus;
                }

                return bus;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private string PrefixQueueName(string name) => $"{_elsaOptions.ServiceBusOptions.QueuePrefix}{name}";
    }
}
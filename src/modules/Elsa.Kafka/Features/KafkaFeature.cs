using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Kafka.Factories;
using Elsa.Kafka.Implementations;
using Elsa.Kafka.Providers;
using Elsa.Kafka.Tasks;
using Elsa.Kafka.UIHints;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Kafka;

public class KafkaFeature(IModule module) : FeatureBase(module)
{
    private Action<KafkaOptions> _configureOptions = _ => { };
    private Func<IServiceProvider, ICorrelationStrategy> _correlationStrategyFactory = sp => sp.GetRequiredService<HeaderCorrelationStrategy>();

    public KafkaFeature ConfigureOptions(Action<KafkaOptions> configureOptions)
    {
        _configureOptions += configureOptions;
        return this;
    }

    public KafkaFeature WithCorrelationStrategy<T>() where T : class, ICorrelationStrategy
    {
        Services.AddScoped<T>();
        _correlationStrategyFactory = sp => sp.GetRequiredService<T>();
        return this;
    }
    
    public KafkaFeature WithCorrelationStrategy(Func<IServiceProvider, ICorrelationStrategy> correlationStrategyFactory)
    {
        _correlationStrategyFactory = correlationStrategyFactory;
        return this;
    }

    public KafkaFeature WithCorrelationStrategy(Func<ICorrelationStrategy> correlationStrategyFactory)
    {
        _correlationStrategyFactory = _ => correlationStrategyFactory();
        return this;
    }

    public KafkaFeature WithCorrelationStrategy(ICorrelationStrategy correlationStrategy)
    {
        _correlationStrategyFactory = _ => correlationStrategy;
        return this;
    }

    public override void Configure()
    {
        Module.AddActivitiesFrom<KafkaFeature>();
    }

    public override void Apply()
    {
        Services.Configure(_configureOptions);

        Services
            .AddBackgroundTask<StartConsumersStartupTask>()
            .AddSingleton<IWorkerManager, WorkerManager>()
            .AddScoped<IWorkerTopicSubscriber, WorkerTopicSubscriber>()
            .AddScoped<OptionsDefinitionProvider>()
            .AddScoped<IConsumerDefinitionProvider>(sp => sp.GetRequiredService<OptionsDefinitionProvider>())
            .AddScoped<IProducerDefinitionProvider>(sp => sp.GetRequiredService<OptionsDefinitionProvider>())
            .AddScoped<ITopicDefinitionProvider>(sp => sp.GetRequiredService<OptionsDefinitionProvider>())
            .AddScoped<ISchemaRegistryDefinitionProvider>(sp => sp.GetRequiredService<OptionsDefinitionProvider>())
            .AddScoped<IConsumerDefinitionEnumerator, ConsumerDefinitionEnumerator>()
            .AddScoped<IProducerDefinitionEnumerator, ProducerDefinitionEnumerator>()
            .AddScoped<ITopicDefinitionEnumerator, TopicDefinitionEnumerator>()
            .AddScoped<ISchemaRegistryDefinitionEnumerator, SchemaRegistryDefinitionEnumerator>()
            .AddScoped<IPropertyUIHandler, ConsumerDefinitionsDropdownOptionsProvider>()
            .AddScoped<IPropertyUIHandler, ProducerDefinitionsDropdownOptionsProvider>()
            .AddScoped<IPropertyUIHandler, TopicDefinitionsDropdownOptionsProvider>()
            .AddScoped<HeaderCorrelationStrategy>()
            .AddScoped<NullCorrelationStrategy>()
            .AddScoped(_correlationStrategyFactory)
            .AddHandlersFrom<KafkaFeature>()
            .AddConsumerFactory<DefaultConsumerFactory>()
            .AddConsumerFactory<ExpandoObjectConsumerFactory>()
            .AddProducerFactory<DefaultProducerFactory>()
            .AddProducerFactory<ExpandoObjectProducerFactory>()
            ;
    }
}
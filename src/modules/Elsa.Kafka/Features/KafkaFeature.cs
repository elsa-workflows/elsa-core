using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
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
            .AddScoped<IConsumerDefinitionProvider, OptionsDefinitionProvider>()
            .AddScoped<IProducerDefinitionProvider, OptionsDefinitionProvider>()
            .AddScoped<ITopicDefinitionProvider, OptionsDefinitionProvider>()
            .AddScoped<IConsumerDefinitionEnumerator, ConsumerDefinitionEnumerator>()
            .AddScoped<IProducerDefinitionEnumerator, ProducerDefinitionEnumerator>()
            .AddScoped<ITopicDefinitionEnumerator, TopicDefinitionEnumerator>()
            .AddScoped<IPropertyUIHandler, ConsumerDefinitionsDropdownOptionsProvider>()
            .AddScoped<IPropertyUIHandler, ProducerDefinitionsDropdownOptionsProvider>()
            .AddScoped<IPropertyUIHandler, TopicDefinitionsDropdownOptionsProvider>()
            .AddScoped<HeaderCorrelationStrategy>()
            .AddScoped<NullCorrelationStrategy>()
            .AddScoped(_correlationStrategyFactory)
            .AddHandlersFrom<KafkaFeature>();
    }
}
# Elsa MassTransit Extensions

This package provides enhanced MassTransit integration for Elsa Workflows, particularly enabling scoped filter registration.

## Problem Solved

The standard Elsa MassTransit integration doesn't provide access to the `IBusRegistrationContext` in the configuration callbacks, which makes it impossible to register scoped middleware/filters.

## Usage Examples

### RabbitMQ with Context

```csharp
elsa.UseMassTransit(massTransit =>
{
    // Use the extension method instead of the standard UseRabbitMq
    massTransit.UseRabbitMqWithContext(rabbitMqConnectionString, rabbit => 
    {
        // Standard configuration still works
        rabbit.ConfigureTransportBus = (context, bus) =>
        {
            bus.PrefetchCount = 50;
            bus.Durable = true;
            bus.AutoDelete = false;
        };
        
        // New capability: Configure using context
        rabbit.ConfigureUsingContext((configurator, context) =>
        {
            // Now you can register scoped filters
            configurator.UseConsumeFilter(typeof(YourScopedFilter<>), context);
            
            // Or access other capabilities that require IBusRegistrationContext
            configurator.ConfigureEndpoints(context);
        });
    });
});
```

### Azure Service Bus with Context

```csharp
elsa.UseMassTransit(massTransit =>
{
    // Use the extension method instead of the standard UseAzureServiceBus
    massTransit.UseAzureServiceBusWithContext(azureServiceBusConnectionString, serviceBus => 
    {
        // Standard configuration still works
        serviceBus.ConfigureTransportBus = (context, bus) =>
        {
            bus.PrefetchCount = 50;
            bus.LockDuration = TimeSpan.FromMinutes(5);
            bus.MaxConcurrentCalls = 32;
        };
        
        // New capability: Configure using context
        serviceBus.ConfigureUsingContext((configurator, context) =>
        {
            // Now you can register scoped filters
            configurator.UseConsumeFilter(typeof(YourScopedFilter<>), context);
            
            // Or access other capabilities that require IBusRegistrationContext
            configurator.ConfigureEndpoints(context);
        });
    });
});
```
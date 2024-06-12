namespace Elsa.AzureServiceBus.ComponentTests;

[CollectionDefinition(nameof(ServiceBusAppCollection))]
public class ServiceBusAppCollection : ICollectionFixture<App>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
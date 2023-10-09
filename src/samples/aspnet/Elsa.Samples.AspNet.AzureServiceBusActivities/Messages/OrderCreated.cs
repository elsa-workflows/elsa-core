namespace Elsa.Samples.AspNet.AzureServiceBusActivities.Messages;

// ReSharper disable once InconsistentNaming
public record OrderCreated(

    string Id,
    string CustomerId,
    string Product,
    int Quantity,
    decimal Total
);
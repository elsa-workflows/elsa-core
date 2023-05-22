namespace Elsa.Samples.MassTransitActivities.Messages;

// ReSharper disable once InconsistentNaming
public record OrderCreated(

    string Id,
    string CustomerId,
    string Product,
    int Quantity,
    decimal Total
);
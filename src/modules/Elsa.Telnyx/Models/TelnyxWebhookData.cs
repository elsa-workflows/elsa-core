namespace Elsa.Telnyx.Models;

// TODO: The Payload property is of type object instead of Payload, this in order for the JSON serializer to serialize derived type properties.
// Once moved to .NET 7, we have more control over polymorphism.
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism?pivots=dotnet-7-0
public record TelnyxWebhookData(string EventType, Guid Id, DateTimeOffset OccurredAt, string RecordType, object Payload) : TelnyxRecord(EventType);
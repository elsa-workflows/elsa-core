using System.Text.Json.Serialization;

namespace Elsa.Telnyx.Client.Models;

public record TelnyxResponse<T>(T Data);

public record CallStatusResponse(
    string CallControlId,
    string CallLegId,
    string CallSessionId,
    string ClientState,
    bool IsAlive,
    string RecordType
);

public record DialResponse(
    string CallControlId,
    string CallLegId,
    string CallSessionId,
    bool IsAlive,
    string RecordType
)
{
    [JsonConstructor]
    public DialResponse() : this(default!, default!, default!, default, default!)
    {
    }
}

public record NumberLookupResponse(
    CallerName CallerName,
    Carrier Carrier,
    string CountryCode,
    string Fraud,
    string NationalFormat,
    string PhoneNumber,
    Portability Portability,
    string RecordType
)
{
    [JsonConstructor]
    public NumberLookupResponse() : this(default!, default!, default!, default!, default!, default!, default!, default!)
    {
    }
}

public record Portability(
    string Altspid,
    string AltspidCarrierName,
    string AltspidCarrierType,
    string City,
    string LineType,
    string Lrn,
    string Ocn,
    string? PortedDate,
    string PortedStatus,
    string Spid,
    string SpidCarrierName,
    string SpidCarrierType,
    string State
)
{
    [JsonConstructor]
    public Portability() : this(default!, default!, default!, default!, default!, default!, default!, default, default!, default!, default!, default!, default!)
    {
    }
}

public record Carrier(
    string ErrorCode,
    string MobileCountryCode,
    string MobileNetworkCode,
    string Name,
    string Type
)
{
    [JsonConstructor]
    public Carrier() : this(default!, default!, default, default!, default!)
    {
    }
}

public record CallerName
{
    [JsonPropertyName("caller_name")] public string Name { get; set; } = default!;
    public string ErrorCode { get; set; } = default!;
}
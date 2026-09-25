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
    public DialResponse() : this(null!, null!, null!, false, null!)
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
    public NumberLookupResponse() : this(null!, null!, null!, null!, null!, null!, null!, null!)
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
    public Portability() : this(null!, null!, null!, null!, null!, null!, null!, null, null!, null!, null!, null!, null!)
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
    public Carrier() : this(null!, null!, null, null!, null!)
    {
    }
}

public record CallerName
{
    [JsonPropertyName("caller_name")] public string Name { get; set; } = null!;
    public string ErrorCode { get; set; } = null!;
}
using Newtonsoft.Json;
using NodaTime;

namespace Elsa.Activities.Telnyx.Client.Models
{
    public record TelnyxResponse<T>(T Data);

    public record DialResponse(string CallControlId, string CallLegId, string CallSessionId, bool IsAlive, string RecordType);

    public record NumberLookupResponse(CallerName CallerName, Carrier Carrier, string CountryCode, string Fraud, string NationalFormat, string PhoneNumber, Portability Portability, string RecordType);

    public record Portability(
        string Altspid,
        string AltspidCarrierName,
        string AltspidCarrierType,
        string City,
        string LineType,
        string Lrn,
        string Ocn,
        LocalDate? PortedDate,
        string PortedStatus,
        string Spid,
        string SpidCarrierName,
        string SpidCarrierType,
        string State);

    public record Carrier(string ErrorCode, string MobileCountryCode, int MobileNetworkCode, string Name, string Type);
    public record CallerName([JsonProperty("caller_name")] string Name, string ErrorCode);
}
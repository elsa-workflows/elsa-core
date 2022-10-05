using Newtonsoft.Json;
using NodaTime;

namespace Elsa.Activities.Telnyx.Client.Models
{
    public class TelnyxResponse<T>
    {
        public T Data { get; set; } = default!;
    }

    public class DialResponse
    {
        public string CallControlId { get; set; } = default!;
        public string CallLegId { get; set; } = default!;
        public string CallSessionId { get; set; } = default!;
        public bool IsAlive { get; set; } = default!;
        public string RecordType { get; set; } = default!;
    }

    public class NumberLookupResponse
    {
        public CallerName CallerName { get; set; } = default!;
        public Carrier Carrier { get; set; } = default!;
        public string CountryCode { get; set; } = default!;
        public string Fraud { get; set; } = default!;
        public string NationalFormat { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public Portability Portability { get; set; } = default!;
        public string RecordType { get; set; } = default!;
    }

    public class Portability
    {
        public string Altspid { get; set; } = default!;
        public string AltspidCarrierName { get; set; } = default!;
        public string AltspidCarrierType { get; set; } = default!;
        public string City { get; set; } = default!;
        public string LineType { get; set; } = default!;
        public string Lrn { get; set; } = default!;
        public string Ocn { get; set; } = default!;
        public LocalDate? PortedDate { get; set; }
        public string PortedStatus { get; set; } = default!;
        public string Spid { get; set; } = default!;
        public string SpidCarrierName { get; set; } = default!;
        public string SpidCarrierType { get; set; } = default!;
        public string State { get; set; } = default!;
    }

    public class Carrier
    {
        public string ErrorCode { get; set; } = default!;
        public string MobileCountryCode { get; set; } = default!;
        public int MobileNetworkCode { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Type { get; set; } = default!;
    }

    public class CallerName
    {
        [JsonProperty("caller_name")] public string Name { get; set; } = default!;
        public string ErrorCode { get; set; } = default!;
    }
}
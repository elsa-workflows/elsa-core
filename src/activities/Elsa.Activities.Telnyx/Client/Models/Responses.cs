using Newtonsoft.Json;
using NodaTime;

namespace Elsa.Activities.Telnyx.Client.Models
{
    public class TelnyxResponse<T>
    {
        [JsonProperty("data")] public T Data { get; set; } = default!;
    }

    public class DialResponse : CallStatus
    {
    }

    public class NumberLookupResponse
    {
        [JsonProperty("caller_name")]public CallerName CallerName { get; set; } = default!;
        [JsonProperty("carrier")]public Carrier Carrier { get; set; } = default!;
        [JsonProperty("country_code")]public string CountryCode { get; set; } = default!;
        [JsonProperty("fraud")]public string Fraud { get; set; } = default!;
        [JsonProperty("national_format")]public string NationalFormat { get; set; } = default!;
        [JsonProperty("p")]public string PhoneNumber { get; set; } = default!;
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
        [JsonProperty("error_code")]public string ErrorCode { get; set; } = default!;
        [JsonProperty("mobile_country_code")]public string MobileCountryCode { get; set; } = default!;
        [JsonProperty("mobile_network_code")]public int MobileNetworkCode { get; set; } = default!;
        [JsonProperty("name")]public string Name { get; set; } = default!;
        [JsonProperty("type")]public string Type { get; set; } = default!;
    }

    public class CallerName
    {
        [JsonProperty("caller_name")] public string Name { get; set; } = default!;
        [JsonProperty("error_code")]public string ErrorCode { get; set; } = default!;
    }

    public class CallStatus
    {
        [JsonProperty("call_control_id")] public string CallControlId { get; set; } = default!;
        [JsonProperty("call_leg_id")] public string CallLegId { get; set; } = default!;
        [JsonProperty("call_session_id")] public string CallSessionId { get; set; } = default!;
        [JsonProperty("is_alive")] public bool IsAlive { get; set; } = default!;
        [JsonProperty("record_type")] public string RecordType { get; set; } = default!;
    }
}
using System;
using System.Text;
using Newtonsoft.Json;

namespace Elsa.Activities.Telnyx.Models
{
    public record ClientStatePayload(string CorrelationId)
    {
        public static ClientStatePayload FromBase64(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            var json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<ClientStatePayload>(json)!;
        }
        
        public string ToBase64()
        {
            var json = JsonConvert.SerializeObject(this);
            var bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }
    }
}
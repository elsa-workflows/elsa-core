using System.Text;
using System.Text.Json;

namespace Elsa.Telnyx.Models
{
    public record ClientStatePayload(string CorrelationId)
    {
        public static ClientStatePayload FromBase64(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<ClientStatePayload>(json)!;
        }
        
        public string ToBase64()
        {
            var json = JsonSerializer.Serialize(this);
            var bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }
    }
}
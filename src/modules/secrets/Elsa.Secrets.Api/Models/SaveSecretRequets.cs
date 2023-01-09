using Elsa.Secrets.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elsa.Secrets.Api.Models
{
    public sealed class SaveSecretRequet
    {
        [JsonProperty("id")]
        public string? SecretId { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public ICollection<SecretProperty> Properties { get; set; } = new List<SecretProperty>();
    }
}

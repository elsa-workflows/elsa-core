using Newtonsoft.Json;

namespace Elsa.Activities.Dropbox.Models
{
    public class UploadMode
    {
        [JsonProperty(PropertyName = ".tag")]
        public UploadModeUnion Tag { get; set; }

        public string Update { get; set; }
    }
}
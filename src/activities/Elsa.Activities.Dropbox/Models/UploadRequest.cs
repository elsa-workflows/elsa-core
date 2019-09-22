using Elsa.Activities.Dropbox.Extensions;
using Newtonsoft.Json;

namespace Elsa.Activities.Dropbox.Models
{
    public class UploadRequest
    {
        public string Path { get; set; }
        public UploadMode Mode { get; set; }

        [JsonProperty(PropertyName = "autorename")]
        public bool AutoRename { get; set; }

        public override string ToString()
        {
            var json = JsonConvert.SerializeObject(this, new JsonSerializerSettings().ConfigureForDropboxApi());

            return json;
        }
    }
}
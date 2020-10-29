using Elsa.Activities.Dropbox.Extensions;
using Newtonsoft.Json;

namespace Elsa.Activities.Dropbox.Models
{
    public class UploadRequest
    {
        public string Path { get; set; }= default!;
        public UploadMode Mode { get; set; }= default!;

        [JsonProperty(PropertyName = "autorename")]
        public bool AutoRename { get; set; }
        
        public override string ToString() => JsonConvert.SerializeObject(this, new JsonSerializerSettings().ConfigureForDropboxApi());
    }
}
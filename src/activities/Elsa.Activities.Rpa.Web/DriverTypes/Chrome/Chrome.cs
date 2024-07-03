using Newtonsoft.Json;

namespace Elsa.Activities.Rpa.Web.DriverTypes.Chrome
{ 
    public class Chrome
    {
        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
using Newtonsoft.Json; 
namespace Elsa.Activities.Rpa.Web.DriverTypes.Chrome{ 

    public class ChromeVersion
    {
        [JsonProperty("version")]
        public string VersionNumber { get; set; }

        [JsonProperty("revision")]
        public string Revision { get; set; }

        [JsonProperty("downloads")]
        public Downloads Downloads { get; set; }
    }

}
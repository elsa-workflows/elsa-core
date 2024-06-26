using Newtonsoft.Json; 
using System.Collections.Generic; 
using System; 
namespace Elsa.Activities.Rpa.Web.DriverTypes.Chrome{ 

    public class ChromeForTesting
    {
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("versions")]
        public List<ChromeVersion> Versions { get; set; }
    }

}
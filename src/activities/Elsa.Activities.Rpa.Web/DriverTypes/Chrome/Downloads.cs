using Newtonsoft.Json; 
using System.Collections.Generic; 
namespace Elsa.Activities.Rpa.Web.DriverTypes.Chrome
{ 
    public class Downloads
    {
        [JsonProperty("chrome")]
        public List<Chrome> Chrome { get; set; }

        [JsonProperty("chromedriver")]
        public List<Chromedriver> Chromedriver { get; set; }
    }
}
using Elsa.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Models
{
    public class Secret : Entity
    {
        public string Type { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string PropertiesJson { get; set; }


        //[NotMapped]
        //public ICollection<SecretProperty> Properties {
        //    get
        //    {
        //        return (ICollection<SecretProperty>)JsonConvert.DeserializeObject(PropertiesJson);
        //    }
        //    set
        //    {
        //        PropertiesJson = JsonConvert.SerializeObject(value);
        //    }
        //}
    }
}

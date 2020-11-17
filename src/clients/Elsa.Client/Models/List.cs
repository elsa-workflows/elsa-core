using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class List<T>
    {
        [DataMember(Order = 1)] public ICollection<T> Items { get; set; } = new System.Collections.Generic.List<T>();
    }
}
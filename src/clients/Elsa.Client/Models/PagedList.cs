using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class PagedList<T>
    {
        [DataMember(Order = 1)] public ICollection<T> Items { get; set; } = new List<T>();
        [DataMember(Order = 2)] public int? Page { get; set; }
        [DataMember(Order = 3)] public int? PageSize { get; set; }
        [DataMember(Order = 4)] public int TotalCount { get; set; }
    }
}
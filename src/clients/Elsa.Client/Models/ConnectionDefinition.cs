using System.Runtime.Serialization;

namespace Elsa.Client.Models
{
    [DataContract]
    public class ConnectionDefinition
    {
        public ConnectionDefinition(string sourceActivityId, string targetActivityId, string outcome)
        {
            SourceActivityId = sourceActivityId;
            TargetActivityId = targetActivityId;
            Outcome = outcome;
        }

        [DataMember(Order = 1)] public string SourceActivityId { get; set; }
        [DataMember(Order = 2)] public string TargetActivityId { get; set; }
        [DataMember(Order = 3)] public string Outcome { get; set; }
    }
}
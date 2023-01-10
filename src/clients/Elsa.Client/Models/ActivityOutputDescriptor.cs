using System.Runtime.Serialization;

namespace Elsa.Client.Models;

[DataContract]
public class ActivityOutputDescriptor
{
    [DataMember(Order = 1)] public string Name { get; set; } = default!;
    [DataMember(Order = 2)] public string Type { get; set; } = default!;
    [DataMember(Order = 3)] public string? Hint { get; set; }
    [DataMember(Order = 4)] public string? DefaultWorkflowStorageProvider { get; set; }
    [DataMember(Order = 5)] public bool DisableWorkflowProviderSelection { get; set; }
    [DataMember(Order = 6)] public bool? IsBrowsable { get; set; }
}
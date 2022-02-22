using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Elsa.Client.Models;

[DataContract]
public class ActivityInputDescriptor
{
    [DataMember(Order = 1)] public string Name { get; set; } = default!;
    [DataMember(Order = 2)] public string Type { get; set; } = default!;
    [DataMember(Order = 3)] public string UIHint { get; set; } = default!;
    [DataMember(Order = 4)] public string Label { get; set; } = default!;
    [DataMember(Order = 5)] public string? Hint { get; set; }
    [DataMember(Order = 6)] public object? Options { get; set; }
    [DataMember(Order = 7)] public string? Category { get; set; }
    [DataMember(Order = 8)] public float Order { get; set; }
    [DataMember(Order = 9)] public object? DefaultValue { get; set; }
    [DataMember(Order = 10)] public string? DefaultSyntax { get; set; }
    [DataMember(Order = 11)] public IList<string> SupportedSyntaxes { get; set; } = new List<string>();
    [DataMember(Order = 12)] public bool? IsReadOnly { get; set; }
    [DataMember(Order = 13)] public bool? IsBrowsable { get; set; }
    [DataMember(Order = 14)] public bool IsDesignerCritical { get; set; }
    [DataMember(Order = 15)] public string? DefaultWorkflowStorageProvider { get; set; }
    [DataMember(Order = 16)] public bool DisableWorkflowProviderSelection { get; set; }
    [DataMember(Order = 17)] public bool ConsiderValuesAsOutcomes { get; set; }
}
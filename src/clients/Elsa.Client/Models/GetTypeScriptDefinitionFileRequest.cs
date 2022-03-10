using System.Runtime.Serialization;

namespace Elsa.Client.Models;

[DataContract]
public class GetTypeScriptDefinitionFileRequest
{
    public GetTypeScriptDefinitionFileRequest(string? activityTypeName, string? propertyName)
    {
        ActivityTypeName = activityTypeName;
        PropertyName = propertyName;
    }

    [DataMember(Order = 1)] public string? ActivityTypeName { get; set; }
    [DataMember(Order = 2)] public string? PropertyName { get; set; }
}

using Refit;

namespace Elsa.Api.Client.Resources.OutputConverters.Requests;

/// <summary>
/// Represents a request to list output converters compatible with declared types.
/// </summary>
public class ListOutputConvertersRequest
{
    /// <summary>
    /// The declared source type name of the activity output.
    /// </summary>
    [AliasAs("sourceType")]
    public string SourceType { get; set; } = null!;

    /// <summary>
    /// The declared destination type name of the output binding.
    /// </summary>
    [AliasAs("destinationType")]
    public string DestinationType { get; set; } = null!;
}

namespace Elsa.ResourceManagement.Serialization.Settings;

/// <summary>
/// Specifies the settings used when merging JSON.
/// </summary>
public class JsonMergeSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonMergeSettings"/> class.
    /// </summary>
    public JsonMergeSettings()
    {
        PropertyNameComparison = StringComparison.Ordinal;
    }

    /// <summary>
    /// Gets or sets the method used when merging JSON arrays.
    /// </summary>
    /// <value>The method used when merging JSON arrays.</value>
    public MergeArrayHandling MergeArrayHandling { get; set; }

    /// <summary>
    /// Gets or sets how null value properties are merged.
    /// </summary>
    public MergeNullValueHandling MergeNullValueHandling { get; set; }

    /// <summary>
    /// Gets or sets the comparison used to match property names while merging.
    /// The exact property name will be searched for first and if no matching property is found then
    /// the <see cref="StringComparison"/> will be used to match a property.
    /// </summary>
    public StringComparison PropertyNameComparison { get; set; }
}

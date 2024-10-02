namespace Elsa.ResourceManagement.Serialization.Settings;

/// <summary>
/// Specifies how null value properties are merged.
/// </summary>
public enum MergeNullValueHandling
{
    /// <summary>
    /// The resource's null value properties will be ignored during merging.
    /// </summary>
    Ignore = 0,

    /// <summary>
    /// The resource's null value properties will be merged.
    /// </summary>
    Merge = 1
}

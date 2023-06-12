using System.ComponentModel;
using System.Text.Json.Serialization;
using Elsa.Common.Converters;
using JetBrains.Annotations;

namespace Elsa.Common.Models;

/// <summary>
/// Provides parameters for querying a given version of versioned entities.
/// </summary>
[TypeConverter(typeof(VersionOptionsTypeConverter))]
[JsonConverter(typeof(VersionOptionsJsonConverter))]
[PublicAPI]
public struct VersionOptions
{
    /// <summary>
    /// Gets the latest version.
    /// </summary>
    public static readonly VersionOptions Latest = new() { IsLatest = true };

    /// <summary>
    /// Gets the published version.
    /// </summary>
    public static readonly VersionOptions Published = new() { IsPublished = true };

    /// <summary>
    /// Gets the published version if available, and if not, the latest version.
    /// </summary>
    public static readonly VersionOptions LatestOrPublished = new() { IsLatestOrPublished = true };

    /// <summary>
    /// Gets the latest, published version.
    /// </summary>
    public static readonly VersionOptions LatestAndPublished = new() { IsLatestOrPublished = true };

    /// <summary>
    /// Gets the draft version.
    /// </summary>
    public static readonly VersionOptions Draft = new() { IsDraft = true };

    /// <summary>
    /// Gets all versions.
    /// </summary>
    public static readonly VersionOptions All = new() { AllVersions = true };

    /// <summary>
    /// Gets a specific version.
    /// </summary>
    public static VersionOptions SpecificVersion(int version) => new() { Version = version };

    /// <summary>
    /// Parses a string into a <see cref="VersionOptions"/>. 
    /// </summary>
    public static VersionOptions FromString(string value) =>
        value switch
        {
            "AllVersions" => All,
            "Draft" => Draft,
            "Latest" => Latest,
            "Published" => Published,
            "LatestOrPublished" => LatestOrPublished,
            "LatestAndPublished" => LatestAndPublished,
            _ => SpecificVersion(int.Parse(value))
        };

    /// <summary>
    /// Tries to parse a string into a <see cref="VersionOptions"/>.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="versionOptions">The parsed <see cref="VersionOptions"/>.</param>
    /// <returns>True if the string could be parsed, otherwise false.</returns>
    public static bool TryParse(string value, out VersionOptions versionOptions)
    {
        versionOptions = FromString(value);
        return true;
    }

    /// <summary>
    /// Gets the latest version.
    /// </summary>
    public bool IsLatest { get; private set; }

    /// <summary>
    /// Gets the published version if available, and if not, the latest version.
    /// </summary>
    public bool IsLatestOrPublished { get; private set; }

    /// <summary>
    /// Gets the latest, published version.
    /// </summary>
    public bool IsLatestAndPublished { get; private set; }

    /// <summary>
    /// Gets the published version.
    /// </summary>
    public bool IsPublished { get; private set; }

    /// <summary>
    /// Gets the draft version.
    /// </summary>
    public bool IsDraft { get; private set; }

    /// <summary>
    /// Gets all versions.
    /// </summary>
    public bool AllVersions { get; private set; }

    /// <summary>
    /// Gets a specific version.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Returns a simple string representation of this <see cref="VersionOptions"/>.
    /// </summary>
    public override string ToString() => AllVersions ? "AllVersions" : IsDraft ? "Draft" : IsLatest ? "Latest" : IsPublished ? "Published" : IsLatestOrPublished ? "LatestOrPublished" : IsLatestAndPublished ? "LatestAndPublished" : Version.ToString();
}
using System.ComponentModel;
using Elsa.Converters;
using Elsa.Serialization.Converters;
using Newtonsoft.Json;

namespace Elsa.Models
{
    [JsonConverter(typeof(VersionOptionsJsonConverter))]
    [TypeConverter(typeof(VersionOptionsTypeConverter))]
    public struct VersionOptions
    {
        /// <summary>
        /// Gets the latest version.
        /// </summary>
        public static readonly VersionOptions Latest = new VersionOptions { IsLatest = true };

        /// <summary>
        /// Gets the published version.
        /// </summary>
        public static readonly VersionOptions Published = new VersionOptions { IsPublished = true };
        
        /// <summary>
        /// Gets the published version if available, and if not, the latest version.
        /// </summary>
        public static readonly VersionOptions LatestOrPublished = new VersionOptions { IsLatestOrPublished = true };

        /// <summary>
        /// Gets the draft version.
        /// </summary>
        public static readonly VersionOptions Draft = new VersionOptions { IsDraft = true };
        
        /// <summary>
        /// Gets all versions.
        /// </summary>
        public static readonly VersionOptions All = new VersionOptions { AllVersions = true };

        /// <summary>
        /// Gets a specific version.
        /// </summary>
        public static VersionOptions SpecificVersion(int version) => new VersionOptions { Version = version };
        
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
                _ => SpecificVersion(int.Parse(value))
            };

        /// <summary>
        /// Gets the latest version.
        /// </summary>
        public bool IsLatest { get; private set; }
        
        /// <summary>
        /// Gets the published version if available, and if not, the latest version.
        /// </summary>
        public bool IsLatestOrPublished { get; private set; }
        
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
        public override string ToString() => AllVersions ? "AllVersions" : IsDraft ? "Draft" : IsLatest ? "Latest" : IsPublished ? "Published" : IsLatestOrPublished ? "LatestOrPublished" : Version.ToString();
    }
}
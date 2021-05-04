namespace Elsa.Client.Models
{
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
        /// Gets the latest or published version.
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

        public bool IsLatest { get; private set; }
        public bool IsLatestOrPublished { get; private set; }
        public bool IsPublished { get; private set; }
        public bool IsDraft { get; private set; }
        public bool AllVersions { get; private set; }
        public int Version { get; private set; }
        
        /// <summary>
        /// Returns a simple string representation of this <see cref="VersionOptions"/>.
        /// </summary>
        public override string ToString() => AllVersions ? "AllVersions" : IsDraft ? "Draft" : IsLatest ? "Latest" : IsPublished ? "Published" : IsLatestOrPublished ? "LatestOrPublished" : Version.ToString();
        
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
    }
}
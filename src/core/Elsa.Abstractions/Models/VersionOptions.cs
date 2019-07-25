namespace Elsa.Models
{
    public struct VersionOptions
    {
        /// <summary>
        /// Gets the latest version.
        /// </summary>
        public static readonly VersionOptions Latest = new VersionOptions { IsLatest = true };
        
        /// <summary>
        /// Gets the latest published version.
        /// </summary>
        public static readonly VersionOptions Published = new VersionOptions { IsPublished = true };
        
        /// <summary>
        /// Gets the latest draft version.
        /// </summary>
        public static readonly VersionOptions Draft = new VersionOptions { IsDraft = true };
        
        /// <summary>
        /// Gets a specific version.
        /// </summary>
        public static VersionOptions SpecificVersion(int version) => new VersionOptions { Version = version };

        public bool IsLatest { get; private set; }
        public bool IsPublished { get; private set; }
        public bool IsDraft { get; private set; }
        public int Version { get; private set; }
    }
}
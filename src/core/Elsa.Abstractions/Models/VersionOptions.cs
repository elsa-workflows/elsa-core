using System;
using System.Linq.Expressions;

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
        
        public static Expression<Func<WorkflowDefinition, bool>> Predicate(VersionOptions version)
        {
            if (version.IsDraft)
                return x => !x.IsPublished;
            if (version.IsLatest)
                return x => x.IsLatest;
            if (version.IsPublished)
                return x => x.IsPublished;
            if (version.Version > 0)
                return x => x.Version == version.Version;
            return x => false;
        }

        public bool IsLatest { get; private set; }
        public bool IsPublished { get; private set; }
        public bool IsDraft { get; private set; }
        public int Version { get; private set; }
    }
}
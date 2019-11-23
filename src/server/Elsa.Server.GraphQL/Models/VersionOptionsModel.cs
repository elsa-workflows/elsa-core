namespace Elsa.Server.GraphQL.Models
{
    public class VersionOptionsModel
    {
        public bool Latest { get; set; }
        public bool LatestOrPublished { get; set; }
        public bool Published { get; set; }
        public bool Draft { get; set; }
        public bool AllVersions { get; set; }
        public int Version { get; set; }
    }
}
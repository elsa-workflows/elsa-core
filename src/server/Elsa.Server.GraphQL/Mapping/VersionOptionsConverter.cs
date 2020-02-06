using AutoMapper;
using Elsa.Models;
using Elsa.Server.GraphQL.Models;

namespace Elsa.Server.GraphQL.Mapping
{
    public class VersionOptionsConverter : ITypeConverter<VersionOptionsInput, VersionOptions>
    {
        public VersionOptions Convert(VersionOptionsInput source, VersionOptions destination, ResolutionContext context)
        {
            if (source.AllVersions == true)
                return VersionOptions.All;
            if (source.Draft == true)
                return VersionOptions.Draft;
            if (source.Latest == true)
                return VersionOptions.Latest;
            if (source.Published == true)
                return VersionOptions.Published;
            if (source.LatestOrPublished == true)
                return VersionOptions.LatestOrPublished;

            return VersionOptions.SpecificVersion(source?.Version ?? 0);
        }
    }
}
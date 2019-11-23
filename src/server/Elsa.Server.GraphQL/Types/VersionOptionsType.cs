using Elsa.Server.GraphQL.Models;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class VersionOptionsInputType : InputObjectGraphType<VersionOptionsModel>
    {
        public VersionOptionsInputType()
        {
            Name = "VersionOptionsInput";

            Field(x => x.Version, true).Description("Get a specific version.");
            Field(x => x.AllVersions, true).Description("Get all versions.");
            Field(x => x.Draft, true).Description("Get draft versions.");
            Field(x => x.Latest, true).Description("Get latest versions, whether draft or published.");
            Field(x => x.Published, true).Description("Get published versions.");
            Field(x => x.LatestOrPublished, true).Description("Get the published versions, or latest even that version is not published.");
        }
    }
}
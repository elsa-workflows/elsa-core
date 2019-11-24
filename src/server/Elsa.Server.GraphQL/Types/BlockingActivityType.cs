using Elsa.Models;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class BlockingActivityType : ObjectGraphType<BlockingActivity>
    {
        public BlockingActivityType()
        {
            Name = "BlockingActivity";

            Field(x => x.ActivityId).Description("The ID of the blocking activity.");
            Field(x => x.ActivityType).Description("The type of the blocking activity");
        }
    }
}
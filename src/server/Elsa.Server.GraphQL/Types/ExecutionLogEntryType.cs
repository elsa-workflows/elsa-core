using Elsa.Models;
using GraphQL.NodaTime;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class ExecutionLogEntryType : ObjectGraphType<ExecutionLogEntry>
    {
        public ExecutionLogEntryType()
        {
            Name = "LogEntry";

            Field(x => x.ActivityId).Description("The ID of the activity that created the entry.");
            Field(x => x.Timestamp, type: typeof(InstantGraphType)).Description("The time stamp the entry was created.");
        }
    }
}
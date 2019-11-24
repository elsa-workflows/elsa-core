using Elsa.Models;
using GraphQL.NodaTime;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class LogEntryType : ObjectGraphType<LogEntry>
    {
        public LogEntryType()
        {
            Name = "LogEntry";

            Field(x => x.ActivityId).Description("The ID of the activity that created the entry.");
            Field(x => x.Message).Description("The message of the entry.");
            Field(x => x.Timestamp, type: typeof(InstantGraphType)).Description("The time stamp the entry was created.");
            Field(x => x.Faulted).Description("A value indicating whether this entry represents a fault message.");
        }
    }
}
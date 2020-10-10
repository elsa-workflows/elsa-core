using YesSql.Indexes;

namespace Elsa.Activities.Http.Indexes
{
    public class WorkflowInstanceByReceiveHttpRequestIndex : MapIndex
    {
        public string ActivityId { get; set; } = default!;
        public string RequestPath { get; set; } = default!;
        public string? RequestMethod { get; set; }
    }
}
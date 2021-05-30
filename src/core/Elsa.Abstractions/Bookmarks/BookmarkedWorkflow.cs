using System.Collections.Generic;
using Elsa.Services.Models;

namespace Elsa.Bookmarks
{
    public class BookmarkedWorkflow
    {
        public IWorkflowBlueprint WorkflowBlueprint { get; set; } = default!;
        public string WorkflowInstanceId { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public IBookmark Bookmark { get; set; } = default!;
    }
}
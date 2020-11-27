using System.Collections.Generic;

namespace Elsa.Samples.ContextualWorkflowHttp.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string DocumentId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Body { get; set; } = default!;
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
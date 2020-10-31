using System.Collections.Generic;

namespace Elsa.Samples.ContextualWorkflowHttp.Models
{
    public class Document
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
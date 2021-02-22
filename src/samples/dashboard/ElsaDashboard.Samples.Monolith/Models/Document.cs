using System;
using System.Collections.Generic;

namespace ElsaDashboard.Samples.Monolith.Models
{
    public class Document
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Author { get; set; } = default!;
        public string? Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public ICollection<Comment> Comments { get; set; }
    }
}
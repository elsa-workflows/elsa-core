using System.Collections.Generic;
using Elsa.Client.Models;
using ElsaDashboard.Models;

namespace ElsaDashboard.Application.Models
{
    public record ActivityModel
    {
        public string ActivityId { get; init; }
        public string Type { get; init; }
        public string? Name { get; init; }
        public string? DisplayName { get; init; }
        public string? Description { get; set; }
        
        public ICollection<string> Outcomes { get; init; } = new List<string>();
        public Variables Properties { get; set; } = new();
        public ActivityDisplayDescriptor? DisplayDescriptor { get; set; }
    }
}
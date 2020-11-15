using System.Collections.Generic;
using System.Linq;

namespace ElsaDashboard.Application.Models
{
    public record ActivityModel
    {
        public ActivityModel(string activityId, string type) => (ActivityId, Type) = (activityId, type);
        public ActivityModel(string activityId, string type, IEnumerable<string> outcomes) => (ActivityId, Type, Outcomes) = (activityId, type, outcomes.ToList());
        public string ActivityId { get; init; }
        public string Type { get; init; }
        public ICollection<string> Outcomes { get; init; } = new List<string>();
    }
}
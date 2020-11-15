namespace ElsaDashboard.Application.Models
{
    public record ActivityModel
    {
        public ActivityModel()
        {
        }
        
        public ActivityModel(string activityId, string type) => (ActivityId, Type) = (activityId, type);
        public string ActivityId { get; init; }
        public string Type { get; init; }
    }
}
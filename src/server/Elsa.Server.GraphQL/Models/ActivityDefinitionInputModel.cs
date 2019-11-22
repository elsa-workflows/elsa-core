using Elsa.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Server.GraphQL.Models
{
    public class ActivityDefinitionInputModel
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public string State { get; set; }

        public ActivityDefinition ToActivityDefinition()
        {
            return new ActivityDefinition
            {
                Id = Id,
                Type = Type,
                Name = Name,
                DisplayName = DisplayName,
                Description = Description,
                Top = Top,
                Left = Left,
                State = JObject.Parse(State ?? "{}")
            };
        }
    }
}
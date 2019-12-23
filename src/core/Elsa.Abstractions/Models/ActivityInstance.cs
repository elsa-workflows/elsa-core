namespace Elsa.Models
{
    public class ActivityInstance
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public Variables State { get; set; }
        public Variable Output { get; set; }
    }
}
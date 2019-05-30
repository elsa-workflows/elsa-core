namespace Elsa.Models
{
    public class SourceEndpoint : Endpoint
    {
        public SourceEndpoint()
        {
        }

        public SourceEndpoint(IActivity activity, string name = EndpointNames.Done) : base(activity)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
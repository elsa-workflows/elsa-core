namespace Flowsharp.Models
{
    public class SourceEndpoint : Endpoint
    {
        public SourceEndpoint()
        {
        }

        public SourceEndpoint(Flowsharp.IActivity activity, string name = null) : base(activity)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
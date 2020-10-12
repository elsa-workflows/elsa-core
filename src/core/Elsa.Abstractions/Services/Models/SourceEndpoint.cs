namespace Elsa.Services.Models
{
    public class SourceEndpoint : Endpoint, ISourceEndpoint
    {
        public SourceEndpoint(IActivityBlueprint activity, string outcome) : base(activity) => Outcome = outcome;
        public string Outcome { get; set; }
    }
}
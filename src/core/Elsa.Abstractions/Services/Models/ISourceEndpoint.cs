namespace Elsa.Services.Models
{
    public interface ISourceEndpoint : IEndpoint
    {
        string Outcome { get; }
    }
}
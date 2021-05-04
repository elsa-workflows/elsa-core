namespace Elsa.Services.Models
{
    public interface IConnection
    {
        ISourceEndpoint Source { get; }
        ITargetEndpoint Target { get; }
    }
}
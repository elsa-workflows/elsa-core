using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IConnectionBuilder
    {
        IActivityBuilder Source { get; }
        IActivityBuilder Target { get; }
        string Outcome { get; }
        Connection BuildConnection();
    }
}
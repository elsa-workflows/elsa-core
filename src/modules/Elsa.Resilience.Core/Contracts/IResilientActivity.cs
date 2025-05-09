using Elsa.Workflows;

namespace Elsa.Resilience;

public interface IResilientActivity : IActivity
{
    string ResilienceCategory { get; }
}
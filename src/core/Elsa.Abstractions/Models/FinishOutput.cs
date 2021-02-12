using System.Collections.Generic;

namespace Elsa.Models
{
    public record FinishOutput(object? Output, IEnumerable<string> Outcomes);
}
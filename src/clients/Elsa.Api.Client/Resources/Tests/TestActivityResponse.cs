using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Resources.Tests;

public class TestActivityResponse
{
    public IDictionary<string, object?>? Outputs { get; set; }
    public ICollection<string>? Outcomes { get; set; }
    public ExceptionState? Exception { get; set; }
    public ActivityStatus Status { get; set; }
}
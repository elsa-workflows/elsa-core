using Elsa.Testing.Framework.Abstractions;
using Elsa.Workflows;

namespace Elsa.Testing.Framework.Assertions;

public class WorkflowStatusAssertion : Assertion
{
    public WorkflowStatus ExpectedStatus { get; set; }
}
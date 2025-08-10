using Elsa.Testing.Framework.Abstractions;
using Elsa.Workflows;

namespace Elsa.Testing.Framework.Assertions;

public class WorkflowSubStatusAssertion : Assertion
{
    public WorkflowSubStatus ExpectedSubStatus { get; set; }
}
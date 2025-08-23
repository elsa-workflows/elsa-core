using Elsa.Testing.Framework.Abstractions;
using Elsa.Testing.Framework.Models;
using Elsa.Workflows.Management;
using JetBrains.Annotations;

namespace Elsa.Testing.Framework.Assertions;

[UsedImplicitly]
public class WorkflowVariableAssertion : Assertion
{
    public string VariableName { get; set; } = null!;
    public object ExpectedValue { get; set; } = null!;

    public override async Task<AssertionResult> RunAsync(AssertionContext context)
    {
        var variableManager = context.GetRequiredService<IWorkflowInstanceVariableManager>();
        var variables = await variableManager.GetVariablesAsync(context.RunWorkflowResult.WorkflowExecutionContext, cancellationToken: context.CancellationToken);
        var variable = variables.FirstOrDefault(x => x.Variable.Name == VariableName);

        if (variable == null)
            return AssertionResult.Fail($"No variable found with name '{VariableName}'.");

        var actualValue = variable.Value;
        var valuesMatch = Equals(ExpectedValue, actualValue);
        return valuesMatch
            ? AssertionResult.Pass()
            : AssertionResult.Fail($"Expected variable '{VariableName}' to have value '{ExpectedValue}', but found '{actualValue}'");
    }
}
using Elsa.Testing.Shared;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace Elsa.Workflows.IntegrationTests.SharedHelpers;

file static class EquivalencyAssertionGeneration
{
    [GenerateAssertion(
        ExpectationMessage = "to be equivalent to {expected} with strict mode set to {strict}",
        InlineMethodBody = true)]
    public static AssertionResult IsEquivalentTo<TActual>(this TActual actual, object? expected, bool strict) =>
        EquivalencyAssertionAdapter.Compare(expected, actual, strict);
}

internal static class EquivalencyAssertionAdapter
{
    public static AssertionResult Compare(object? expected, object? actual, bool strict)
    {
        try
        {
            TestAssert.Equivalent(expected, actual, strict);
            return AssertionResult.Passed;
        }
        catch (TestAssertionException exception)
        {
            return AssertionResult.Failed(exception.Message);
        }
    }
}

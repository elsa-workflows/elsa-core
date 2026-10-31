using Elsa.Bpmn.Hosting;

namespace Elsa.Bpmn.UnitTests;

/// <summary>
/// <see cref="BpmnScopeHost.TryGetDiagnosticSequence"/> is the only thing standing between an interpreter-minted
/// diagnostic id and the durable cursor <see cref="BpmnScopeHost"/> uses to avoid re-projecting one twice. A
/// malformed id that parsed anyway -- an arbitrary prefix, or a number the library would never actually mint --
/// could poison that cursor with a value too high, silently skipping every later, genuinely valid, lower-sequence
/// diagnostic forever. These tests pin down exactly what "parses" means: the ordinal prefix <c>diag:</c>, verbatim,
/// followed by a non-negative integer with no sign, grouping or surrounding whitespace.
/// </summary>
public class BpmnScopeHostDiagnosticSequenceTests
{
    [Theory]
    [InlineData("diag:5", 5)]
    [InlineData("diag:0", 0)]
    [InlineData("diag:12345", 12345)]
    public void TryGetDiagnosticSequence_WithAWellFormedId_ReturnsItsOrdinal(string diagnosticId, int expectedSequence)
    {
        var parsed = BpmnScopeHost.TryGetDiagnosticSequence(diagnosticId, out var sequence);

        Assert.True(parsed);
        Assert.Equal(expectedSequence, sequence);
    }

    [Theory]
    [InlineData("999")]
    [InlineData("foreign:999")]
    [InlineData("diag:-1")]
    [InlineData("diag:")]
    [InlineData("diag:abc")]
    [InlineData("diag: 5")]
    public void TryGetDiagnosticSequence_WithAMalformedId_IsRejected(string diagnosticId)
    {
        var parsed = BpmnScopeHost.TryGetDiagnosticSequence(diagnosticId, out var sequence);

        Assert.False(parsed);
        Assert.Equal(0, sequence);
    }
}

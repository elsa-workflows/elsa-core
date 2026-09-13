using Elsa.Bpmn.Hosting;
using System.Threading.Tasks;

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
    [Test]
    [Arguments("diag:5", 5)]
    [Arguments("diag:0", 0)]
    [Arguments("diag:12345", 12345)]
    public async Task TryGetDiagnosticSequence_WithAWellFormedId_ReturnsItsOrdinal(string diagnosticId, int expectedSequence)
    {
        var parsed = BpmnScopeHost.TryGetDiagnosticSequence(diagnosticId, out var sequence);

        await Assert.That(parsed).IsTrue();
        await Assert.That(sequence).IsEqualTo(expectedSequence);
    }

    [Test]
    [Arguments("999")]
    [Arguments("foreign:999")]
    [Arguments("diag:-1")]
    [Arguments("diag:")]
    [Arguments("diag:abc")]
    [Arguments("diag: 5")]
    public async Task TryGetDiagnosticSequence_WithAMalformedId_IsRejected(string diagnosticId)
    {
        var parsed = BpmnScopeHost.TryGetDiagnosticSequence(diagnosticId, out var sequence);

        await Assert.That(parsed).IsFalse();
        await Assert.That(sequence).IsEqualTo(0);
    }
}
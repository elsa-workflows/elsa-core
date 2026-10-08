using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Elsa.Bpmn.Interchange.Services;

/// <summary>
/// The one place a workflow definition's serialized activity graph
/// (<see cref="Elsa.Workflows.Management.Entities.WorkflowDefinition.StringData"/>) is turned into a content hash,
/// shared by <see cref="BpmnInterchangeDocumentService"/> — which records the hash at import time under
/// <see cref="BpmnInterchangeDocumentService.SourceGraphHashCustomPropertyKey"/> to tell a designer-edited graph
/// apart from the one BPMN source was imported against — and
/// <see cref="Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document.BpmnDocumentETag"/>, so the two never disagree about how
/// the same field hashes.
/// </summary>
internal static class BpmnContentHash
{
    /// <summary>The hex-encoded SHA-256 of a workflow definition's serialized activity graph.</summary>
    public static string OfGraph(string? stringData)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendField(hash, stringData);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    /// <summary>
    /// Appends a length-prefixed UTF-8 encoding of <paramref name="value"/> to <paramref name="hash"/>, marking a
    /// <c>null</c> value distinctly from an empty one so no two different inputs feed the hash the same bytes.
    /// </summary>
    public static void AppendField(IncrementalHash hash, string? value)
    {
        Span<byte> header = stackalloc byte[5];

        if (value is null)
        {
            header[0] = 0;
            hash.AppendData(header[..1]);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        header[0] = 1;
        BinaryPrimitives.WriteInt32BigEndian(header[1..], bytes.Length);
        hash.AppendData(header);
        hash.AppendData(bytes);
    }
}

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Data;

namespace Elsa.Agents.Skills;

public class DocumentQuerySkill
{
    [Experimental("SKEXP0001")]
    [KernelFunction("query_document")]
    [Description("Queries a document using vector search.")]
    public async Task<string[]> QueryDocumentAsync(
        Kernel kernel,
        [Description("The document to query.")] string documentText,
        [Description("The query to perform on the document.")] string query,
        CancellationToken cancellationToken = default)
    {
        var embeddingService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var vectorStore = new InMemoryVectorStore(new() { EmbeddingGenerator = embeddingService });
        var collection = vectorStore.GetCollection<string, Document>("documents");
        var document = new Document
        {
            Key = Guid.NewGuid().ToString(),
            Text = documentText,
            TextEmbedding = await embeddingService.GenerateAsync(documentText, cancellationToken: cancellationToken)
        };
        await collection.EnsureCollectionExistsAsync(cancellationToken);
        await collection.UpsertAsync(document, cancellationToken);

        var vectorSearch = new VectorStoreTextSearch<Document>(collection);
        var results = await vectorSearch.SearchAsync(query, cancellationToken: cancellationToken);
        var results2 = await results.Results.ToArrayAsync(cancellationToken: cancellationToken);

        return results2;
    }
}

public class DocumentQuerySkillsProvider : SkillsProvider
{
    public override IEnumerable<SkillDescriptor> GetSkills()
    {
        yield return SkillDescriptor.From<DocumentQuerySkill>();
    }
}

public class Document
{
    [VectorStoreKey] [TextSearchResultName] public string Key { get; set; } = Guid.NewGuid().ToString();
    [VectorStoreData] [TextSearchResultValue] public string Text { get; set; } = null!;
    [VectorStoreVector(1536)] public Embedding<float> TextEmbedding { get; set; } = null!;
}
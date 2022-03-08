using Elsa.Activities.Console;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.WorkflowContexts.Abstractions;
using Elsa.Modules.WorkflowContexts.Extensions;
using Elsa.Modules.WorkflowContexts.Models;
using Elsa.Runtime.Contracts;

namespace Elsa.Samples.Web1.Workflows;

public class WorkflowContextsWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        var documentContext = new WorkflowContext<Document, DocumentProvider>();
        
        workflow
            .AddWorkflowContext(documentContext)
            .WithRoot(new WriteLine(context => $"Document title: {documentContext.Get(context)!.Title}"));
    }
}

public class DocumentProvider : WorkflowContextProvider<Document>
{
    protected override Document? Load(WorkflowExecutionContext workflowExecutionContext)
    {
        var idGenerator = workflowExecutionContext.GetRequiredService<IIdentityGenerator>();

        return new Document
        {
            Id = idGenerator.GenerateId(),
            Title = "Requirements",
            Body = "Workflows should be able to load contextual data easily"
        };
    }
}

public class Document
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
}
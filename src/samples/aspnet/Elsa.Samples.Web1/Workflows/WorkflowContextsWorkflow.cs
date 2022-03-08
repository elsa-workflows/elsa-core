using Elsa.Activities.Console;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.WorkflowContexts.Abstractions;
using Elsa.Modules.WorkflowContexts.Extensions;
using Elsa.Runtime.Contracts;

namespace Elsa.Samples.Web1.Workflows;

public class WorkflowContextsWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        var documentContext = workflow.CreateWorkflowContext<Document, DocumentProvider>();
        var customerContext = workflow.CreateWorkflowContext<Customer, CustomerProvider>();
        
        workflow
            .WithRoot(new Sequence
            {
                Activities =
                {
                    new WriteLine(context => $"Document title: {documentContext.Get(context)!.Title}"),
                    new WriteLine(context => $"Customer name: {customerContext.Get(context)!.Name}"),
                }
            });
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

public class CustomerProvider : WorkflowContextProvider<Customer>
{
    protected override Customer? Load(WorkflowExecutionContext workflowExecutionContext)
    {
        var idGenerator = workflowExecutionContext.GetRequiredService<IIdentityGenerator>();

        return new Customer
        {
            Id = idGenerator.GenerateId(),
            Name = "Joanna",
        };
    }
}

public class Document
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
}

public class Customer
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
}
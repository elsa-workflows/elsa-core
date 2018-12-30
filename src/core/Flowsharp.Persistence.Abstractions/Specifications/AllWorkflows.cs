using Flowsharp.Models;
using Flowsharp.Persistence.Specifications.Primitives;

namespace Flowsharp.Persistence.Specifications
{
    public class AllWorkflows : All<Workflow, IWorkflowSpecificationVisitor>
    {
    }
}
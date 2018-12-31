using Elsa.Models;
using Elsa.Persistence.Specifications.Primitives;

namespace Elsa.Persistence.Specifications
{
    public class AllWorkflows : All<Workflow, IWorkflowSpecificationVisitor>
    {
    }
}
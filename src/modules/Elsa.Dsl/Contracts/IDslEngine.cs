using Elsa.Workflows.Core.Models;

namespace Elsa.Dsl.Contracts;

public interface IDslEngine
{
    Workflow Parse(string script);
}
using Elsa.Workflows.Core.Models;

namespace Elsa.Dsl.Services;

public interface IDslEngine
{
    Workflow Parse(string script);
}
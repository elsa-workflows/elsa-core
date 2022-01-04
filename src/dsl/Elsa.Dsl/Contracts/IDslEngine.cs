using Elsa.Models;

namespace Elsa.Dsl.Contracts;

public interface IDslEngine
{
    Workflow Parse(string script);
}
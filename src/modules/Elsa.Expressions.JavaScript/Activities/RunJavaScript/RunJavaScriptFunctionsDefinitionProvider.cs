using Elsa.Expressions.JavaScript.TypeDefinitions.Abstractions;
using Elsa.Expressions.JavaScript.TypeDefinitions.Models;
using Elsa.Workflows.Helpers;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Expressions.JavaScript.Activities;

/// <summary>
/// Produces <see cref="FunctionDefinition"/>s for common functions.
/// </summary>
[UsedImplicitly]
internal class RunJavaScriptFunctionsDefinitionProvider : FunctionDefinitionProvider
{
    protected override IEnumerable<FunctionDefinition> GetFunctionDefinitions(TypeDefinitionContext context)
    {
        if (context.ActivityTypeName != ActivityTypeNameHelper.GenerateTypeName<RunJavaScript>())
            yield break;
        
        if(context.PropertyName != nameof(RunJavaScript.Script))
            yield break;
        
        yield return CreateFunctionDefinition(builder => builder
            .Name("setOutcome")
            .Parameter("name", "string")
            .ReturnType("void"));
        
        yield return CreateFunctionDefinition(builder => builder
            .Name("setOutcomes")
            .Parameter("names", "string[]")
            .ReturnType("void"));
    }
}
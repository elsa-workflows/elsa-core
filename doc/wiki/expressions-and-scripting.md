# Expressions And Scripting

Expressions let workflow inputs be dynamic. The base expression feature provides evaluator infrastructure; language modules add concrete evaluators, descriptors, activities, and type/function definitions.

## Base Expressions

[ExpressionsFeature](../../src/modules/Elsa.Expressions/Features/ExpressionsFeature.cs) registers:

- `IExpressionEvaluator`
- `IWellKnownTypeRegistry`

The base project is [Elsa.Expressions](../../src/modules/Elsa.Expressions). It is intentionally small and does not own language-specific runtime behavior.

## Language Modules

| Module | Feature | Evaluator | Notes |
| --- | --- | --- | --- |
| [Elsa.Expressions.JavaScript](../../src/modules/Elsa.Expressions.JavaScript) | [JavaScriptFeature](../../src/modules/Elsa.Expressions.JavaScript/Features/JavaScriptFeature.cs) | Jint-backed `IJavaScriptEvaluator` | Adds type definitions, function definitions, `RunJavaScript`, and FastEndpoints assembly. |
| [Elsa.Expressions.CSharp](../../src/modules/Elsa.Expressions.CSharp) | [CSharpFeature](../../src/modules/Elsa.Expressions.CSharp/Features/CSharpFeature.cs) | Roslyn scripting-backed `ICSharpEvaluator` | Adds `RunCSharp`, descriptors, and C# options. |
| [Elsa.Expressions.Python](../../src/modules/Elsa.Expressions.Python) | [PythonFeature](../../src/modules/Elsa.Expressions.Python/Features/PythonFeature.cs) | pythonnet-backed `IPythonEvaluator` | Registers `PythonGlobalInterpreterManager` as a hosted service. |
| [Elsa.Expressions.Liquid](../../src/modules/Elsa.Expressions.Liquid) | [LiquidFeature](../../src/modules/Elsa.Expressions.Liquid/Features/LiquidFeature.cs) | Fluid-backed Liquid manager | Adds Liquid filters and parser services. |

## JavaScript

JavaScript is the richest expression module. It registers:

- `IJavaScriptEvaluator`
- `ITypeDefinitionService`
- type describers and type definition renderers
- function definition providers
- variable definition providers
- `RunJavaScript` activity
- TypeScript definition support
- expression descriptors for Studio

Configuration example from [Elsa.Server.Web/Program.cs](../../src/apps/Elsa.Server.Web/Program.cs):

```csharp
elsa.UseJavaScript(options =>
{
    options.AllowClrAccess = true;
    options.ConfigureEngine(engine =>
    {
        engine.Execute("function greet(name) { return `Hello ${name}!`; }");
    });
});
```

Additional JavaScript libraries are in [Elsa.Expressions.JavaScript.Libraries](../../src/modules/Elsa.Expressions.JavaScript.Libraries), including Lodash, Lodash FP, and Moment feature packages.

## CSharp

[CSharpFeature](../../src/modules/Elsa.Expressions.CSharp/Features/CSharpFeature.cs) registers C# descriptors and `ICSharpEvaluator`, then adds activities from its assembly. The reference server demonstrates configuring wrappers and appending helper scripts:

```csharp
elsa.UseCSharp(options =>
{
    options.AllowHostCodeExecution = true;
    options.DisableWrappers = disableVariableWrappers;
    options.AppendScript("string Greet(string name) => $\"Hello {name}!\";");
});
```

Roslyn C# scripting is privileged host-code execution, not a sandbox. Hosts must explicitly set `CSharpOptions.AllowHostCodeExecution` to `true` before C# expressions or `RunCSharp` can be authored or executed. API callers that author, publish, dispatch, or directly execute workflows containing C# must have the `exec:csharp-expressions` permission.

## Python

[PythonFeature](../../src/modules/Elsa.Expressions.Python/Features/PythonFeature.cs) registers pythonnet-based evaluation and configures `PythonGlobalInterpreterManager` as a hosted service. Python.NET execution is privileged host-code execution, not a sandbox. Python code can access host process capabilities through pythonnet and must only be enabled for trusted workflow authors.

Hosts must explicitly set `PythonOptions.AllowHostCodeExecution` to `true` before Python expressions or `RunPython` can be authored or executed. API callers that author, publish, dispatch, or directly execute workflows containing Python must have the `exec:python-expressions` permission. Hosts must also configure the Python DLL path or set `PYTHONNET_PYDLL`.

The reference server binds `Scripting:Python` configuration in [Program.cs](../../src/apps/Elsa.Server.Web/Program.cs).

Python.NET hardening is part of the broader script execution security tracking in [#7096](https://github.com/elsa-workflows/elsa-core/issues/7096).

## Liquid

[LiquidFeature](../../src/modules/Elsa.Expressions.Liquid/Features/LiquidFeature.cs) registers Fluid options, parser services, expression descriptors, and built-in filters:

- array filters
- string filters
- number filters
- miscellaneous filters
- `base64`
- `keys`

The reference server configures the Fluid encoder to `HtmlEncoder.Default`.

## Expression Descriptors

Expression descriptors let Studio know which expression languages are available and how to present them. Providers are registered by language features, for example:

- `JavaScriptExpressionDescriptorProvider`
- `CSharpExpressionDescriptorProvider`
- `PythonExpressionDescriptorProvider`
- `LiquidExpressionDescriptorProvider`

The API exposes descriptors under `/elsa/api/descriptors/expression-descriptors`.

## Type Aliases

Expression modules and activity modules register type aliases through `ExpressionOptions`. HTTP, for example, adds aliases such as `HttpRequest`, `HttpResponse`, `RouteData`, `FormFile`, and `Downloadable` in [HttpFeature](../../src/modules/Elsa.Http/Features/HttpFeature.cs).

## ElsaScript Relationship

ElsaScript does not replace expression languages. It uses Elsa's expression providers through language prefixes such as `js =>`, `cs =>`, `py =>`, and `liquid =>`. See [ElsaScript README](../../src/modules/Elsa.Dsl.ElsaScript/README.md).

## Testing

Expression tests are split by concern:

- [test/unit/Elsa.Expressions.UnitTests](../../test/unit/Elsa.Expressions.UnitTests)
- [test/integration/Elsa.JavaScript.IntegrationTests](../../test/integration/Elsa.JavaScript.IntegrationTests)
- [test/integration/Elsa.Dsl.ElsaScript.IntegrationTests](../../test/integration/Elsa.Dsl.ElsaScript.IntegrationTests)
- workflow integration tests under [test/integration/Elsa.Workflows.IntegrationTests/Evaluation](../../test/integration/Elsa.Workflows.IntegrationTests/Evaluation)

Prefer unit tests for parser/evaluator behavior and integration tests when expression evaluation interacts with workflow variables, activity outputs, or designer descriptors.

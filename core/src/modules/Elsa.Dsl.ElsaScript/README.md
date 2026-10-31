# ElsaScript DSL Implementation

This document provides an overview of the ElsaScript DSL implementation for Elsa Workflows.

## Overview

ElsaScript is a JavaScript-inspired textual DSL for authoring Elsa 3 workflows. It provides a concise, code-centric alternative to C# WorkflowBuilder APIs and JSON workflow definitions.

## Features Implemented

### Parser (`ElsaScriptParser`)

The parser uses a simplified regex-based approach to parse ElsaScript source code into an Abstract Syntax Tree (AST).

**Supported Syntax:**
- `use` statements for namespaces and expression language configuration
- `workflow "Name" { ... }` declarations
- Variable declarations: `var`, `let`, `const`
- Activity invocations with positional and named arguments
- Expression literals (strings, numbers, booleans, arrays)
- Expression language prefixes (`=>`, `js =>`, `cs =>`, `py =>`, `liquid =>`)
- `listen` keyword for workflow triggers (partial support)

### AST Nodes

Complete set of AST node types:
- `WorkflowNode` - Root workflow definition
- `StatementNode` base class with implementations:
  - `VariableDeclarationNode`
  - `ActivityInvocationNode`
  - `BlockNode` (for sequences)
  - `IfNode`, `ForEachNode`, `ForNode`, `WhileNode`, `SwitchNode`
  - `FlowchartNode`, `ListenNode`
- `ExpressionNode` base class with implementations:
  - `LiteralNode`, `IdentifierNode`, `ArrayLiteralNode`
  - `ElsaExpressionNode` (with language support)
  - `TemplateStringNode`, `BinaryExpressionNode`, `UnaryExpressionNode`
- `ArgumentNode` for activity parameters
- `UseNode` for import/configuration statements

### Compiler (`ElsaScriptCompiler`)

Compiles AST to Elsa workflow activities:
- Traverses AST and constructs Elsa activity graphs
- Maps DSL constructs to Elsa activities:
  - Blocks → `Sequence`
  - Variables → `Variable` instances
  - Activity invocations → Elsa activity instances
  - Control flow nodes → corresponding Elsa activities
- Resolves activities via `IActivityRegistry`
- Binds expressions to Elsa expression system with language support
- Maps expression languages (js, cs, py, liquid) to Elsa providers

### Dependency Injection

Integration with Elsa's module system:
- `ElsaScriptFeature` for service registration
- `ModuleExtensions.UseElsaScript()` for easy setup
- Registers `IElsaScriptParser` and `IElsaScriptCompiler` services

## Example Usage

```elsa
use Elsa.Activities.Console;
use expressions js;

workflow "HelloWorld" {
  var greeting = "Hello";
  WriteLine(=> greeting + " World");
  WriteLine("Great to meet you!");
}
```

## Integration Tests

Six integration tests demonstrate the implementation:

1. **Parser can parse a simple workflow definition** - Verifies basic workflow parsing
2. **Parser can parse variable declarations** - Tests var/let/const parsing
3. **Parser can parse activity invocations with named arguments** - Tests argument parsing
4. **Compiler service is registered and available** - Verifies DI setup
5. **Compiler can parse and analyze a workflow AST** - Tests AST analysis
6. **Compiler recognizes variable declarations in AST** - Tests variable compilation

## Known Limitations

This is a foundation implementation with several areas for future enhancement:

### Parser Limitations
1. Uses regex instead of Parlot for parsing (simpler but less robust)
2. Control flow parsing (if/foreach/while/for/switch) needs completion
3. Block/sequence parsing needs enhancement
4. Flowchart syntax not yet implemented
5. Template string parsing incomplete

### Compiler Limitations
1. Dynamic activity instantiation needs refinement
2. Control flow compilation stubs need full implementation
3. For loop compilation not implemented
4. Flowchart compilation not implemented
5. Error handling and diagnostics minimal

### Runtime Limitations
1. End-to-end workflow execution not fully working
2. Activity property setting needs enhancement
3. Complex expression evaluation untested

## Future Enhancements

### Short Term
1. Complete parser using Parlot for robust parsing
2. Implement full control flow parsing and compilation
3. Fix dynamic activity instantiation for runtime execution
4. Add comprehensive error messages and diagnostics

### Medium Term
1. Implement flowchart parsing and compilation
2. Support template strings with interpolation
3. Add output capture syntax (`let result = Activity()`)
4. Implement switch fallthrough options
5. Add try/catch/finally support

### Long Term
1. IDE support (syntax highlighting, IntelliSense)
2. Debugging support
3. Performance optimizations
4. Extended DSL features (custom operators, macros, etc.)

## Testing Strategy

Current tests focus on:
- Parser correctness for basic constructs
- AST structure validation
- Compiler service availability
- Variable and activity node recognition

Additional testing needed for:
- Control flow execution
- Expression evaluation
- Error handling
- Edge cases and invalid syntax

## Architecture Decisions

### Why Regex-Based Parser?
The simplified regex-based parser was chosen to deliver a working proof-of-concept quickly. While less robust than a full Parlot implementation, it demonstrates the DSL concept and can be replaced with a more sophisticated parser later.

### Why Separate AST and Compiler?
The separation allows for:
- Multiple compilation targets (if needed)
- AST analysis and transformation
- Better error reporting
- Easier testing of each component

### Why Reuse Elsa's Expression System?
Rather than creating a new expression language, ElsaScript wraps Elsa's existing expression providers (JavaScript, C#, Python, Liquid). This provides:
- Consistency with existing Elsa workflows
- Proven expression evaluation
- No additional dependencies
- Familiar syntax for Elsa users

## Files Added

### Source Files
- `src/modules/Elsa.Dsl.ElsaScript/`
  - `Ast/` - AST node definitions
  - `Compiler/` - AST to Elsa workflow compiler
  - `Contracts/` - Service interfaces
  - `Extensions/` - DI extension methods
  - `Features/` - Elsa feature module
  - `Parser/` - ElsaScript parser
  - `Elsa.Dsl.ElsaScript.csproj` - Project file

### Test Files
- `test/integration/Elsa.Dsl.ElsaScript.IntegrationTests/`
  - `ParserTests.cs` - Parser integration tests
  - `CompilerTests.cs` - Compiler integration tests
  - `Elsa.Dsl.ElsaScript.IntegrationTests.csproj` - Test project

### Configuration
- `Directory.Packages.props` - Added Parlot package reference

## Summary

The ElsaScript DSL provides a solid foundation for text-based workflow authoring in Elsa 3. While this initial implementation has limitations, it demonstrates the core concepts and provides a framework for future enhancements. The modular architecture integrates cleanly with Elsa's existing systems and can be extended incrementally.

# Expressions and completion

Activity inputs offer the expression syntaxes installed on the connected server.
C# uses CodeMirror's C# syntax mode; JavaScript and Python retain their existing
language modes. Completion is available in both expression inputs and script/code
inputs. Read-only inputs remain non-editable.

Press Ctrl+Space to request suggestions. Current variables, workflow inputs and
activity outputs are available before a save. Suggestions use the runtime's own
accessors: `Variable.Customer`/`Input.Customer`/`Output.From(...)` for C#,
`variables.customer`/`getInput(...)`/`getOutputFrom(...)` for JavaScript, and
`variables.customer`/`input.Get(...)`/`output.Get(...)` for Python. Generic accessors
quote names that cannot form identifiers. Liquid retains its existing workflow
member suggestions. These changes do not rewrite stored expressions.

The editor also requests the existing server endpoint
`scripting/javascript/type-definitions/{definitionId}` with the activity type and
property name. Its declaration document describes .NET types as well as JavaScript
globals. The editor uses those type members for C#, JavaScript and Python while
offering JavaScript-specific global functions only in JavaScript. It reads the
server's declaration format without executing it. Typed member suggestions cover
one level; this is metadata completion rather than a C# language-server diagnostic
or overload-resolution service.

Type information is cached by API/query scope, definition, activity type and
property for one minute, and invalidated after a workflow save. Typing does not
request it again. Requests support cancellation. The JavaScript scripting feature
and its view permission are needed for that server endpoint even when editing C#
or Python. An unavailable endpoint, rejected permission or declaration larger than
2 MB displays a non-blocking status and leaves local workflow member completion
usable. The local suggestions reflect current editor state; server descriptions
reflect the saved definition and the server's installed type providers.

Input wrapper names preserve the input's exact case (`customer_id` uses
`getcustomer_id()`). Cached declarations do not revive removed or renamed workflow
wrappers. Because the server document does not identify function ownership, unknown
`get*`/`set*` functions are omitted unless they match current workflow accessors or
the server's stable common helpers. Custom providers can still offer other function
names; omitted functions remain usable when typed manually. Generic CLR type names
such as ``List`1`` are read in the same format emitted by the server.

## Performance check

Run `pnpm benchmark:expressions` to reproduce the #8854 completion-generation
measurement. It builds a context with 3,000 variables, 3,000 inputs and 10,000
activities, warms up once and reports the median of five runs. On the development
environment with Node 24.19 and localized descriptions, C# produced 38,000 suggestions
in 95.8 ms, JavaScript 47,000 in 79.6 ms, and Python 35,000 in 63.8 ms. This measures completion generation;
it is not a measurement of rendering an entire workflow diagram.

Type parsing and completion construction are memoized, and the CodeMirror extension
prepares option objects once per completion set. Unit coverage includes the final
output of the 10,000th activity. Browser coverage checks syntax highlighting,
server members, inputs/outputs in all three languages, request reuse, read-only
mode, server-failure fallback and saving while a type request remains pending. CI reports the benchmark without asserting a
hardware-dependent time limit.

C# name-based input completion uses `Input.Get<object>("Name")`: the runtime
requires the explicit type argument to `Get<T>`. Generated `Input.Name` properties
remain available for valid identifiers and retain their declared types.

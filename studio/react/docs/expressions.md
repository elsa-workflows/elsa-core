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

Generated variable wrappers and C# input properties require alphanumeric names,
matching the runtime validator; an underscore is not sufficient just because it
forms a valid language identifier. Use `Variable.Get("customer_id")`,
`Input.Get<object>("input_id")` or JavaScript `getInput("input_id")` for those names.
Usable JavaScript input wrapper names preserve exact case (`customer` uses
`getcustomer()`). The data-usage index recognizes generated PascalCase variable
properties/getters/setters and generic C# accessors as well as legacy forms.
A plain property assignment is a write; a reference on its right-hand side or in
a comparison remains a read. Compound assignments and prefix/postfix increment or
decrement count as both a read and a write.
Typed variable/input bindings accept picker JSON, reference objects and legacy
raw ids/names. Their descriptions and other reference metadata remain data rather
than executable expressions; explicit variable ids take precedence over names.
Name-based usage hints still use workflow-wide matching; sibling-scope attribution
is tracked in [#8978](https://github.com/elsa-workflows/elsa-core/issues/8978).
Cached declarations do not revive removed or renamed workflow
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
remain available for runtime-supported alphanumeric names and retain their declared types.

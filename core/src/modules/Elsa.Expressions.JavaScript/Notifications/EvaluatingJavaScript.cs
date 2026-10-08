using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Jint;

namespace Elsa.Expressions.JavaScript.Notifications;

/// <summary>
/// This notification is published every time a JavaScript expression is about to be evaluated.
/// It gives subscribers a chance to configure the <see cref="Engine"/> with additional functions and variables.
/// </summary>
/// <param name="Engine">The engine the expression will be evaluated on.</param>
/// <param name="Context">The context the expression is evaluated in.</param>
/// <param name="Expression">The expression that is about to be evaluated.</param>
/// <param name="ReferencedGlobals">
/// The identifiers the expression references without declaring them, as reported by the parser. A handler that
/// would otherwise install a global speculatively can register only the names in this set: an identifier the
/// expression never mentions cannot be read from it.
/// <para>
/// Four cases are not answerable from the set and must be treated as "register everything":
/// <see cref="Jint.ReferencedGlobals.HasDirectEvalCall"/>, because evaluated code can reference anything; the
/// identifiers <c>eval</c> and <c>Function</c> appearing in the set, which is how indirect <c>eval</c> and the
/// <c>Function</c> constructor are signalled (neither is reported as a direct <c>eval</c> call); and
/// <c>globalThis</c>, because a global can be reached through it without its name appearing as an identifier.
/// </para>
/// <para>
/// Also note that only free identifiers are reported, so the member names in <c>variables.Foo</c> are not: the
/// set says whether <c>variables</c> is referenced, not which of its properties.
/// </para>
/// <see langword="null"/> when the expression was prepared without collecting them.
/// </param>
public record EvaluatingJavaScript(Engine Engine, ExpressionExecutionContext Context, string Expression, ReferencedGlobals? ReferencedGlobals = null) : INotification;
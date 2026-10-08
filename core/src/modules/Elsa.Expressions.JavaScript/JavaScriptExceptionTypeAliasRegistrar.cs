using System.Reflection;
using Acornima;
using Elsa.Expressions.Options;
using Elsa.Workflows.Options;
using Jint;
using Jint.Runtime;
using Elsa.Common.Serialization;

namespace Elsa.Expressions.JavaScript;

internal static class JavaScriptExceptionTypeAliasRegistrar
{
    public static void Register(ExpressionOptions options)
    {
        options.RegisterTypeAlias(typeof(ScriptPreparationException), nameof(ScriptPreparationException));
        options.RegisterTypeAlias(typeof(JavaScriptException), nameof(JavaScriptException));
        options.RegisterTypeAlias(typeof(SyntaxErrorException), nameof(SyntaxErrorException));

        var wrapperExceptionType = typeof(JavaScriptException).GetNestedType("JavaScriptErrorWrapperException", BindingFlags.Public | BindingFlags.NonPublic);
        if (wrapperExceptionType != null)
            options.RegisterTypeAlias(wrapperExceptionType, "Jint.JavaScriptErrorWrapperException");
    }

    public static void Register(SerializationTypeOptions options)
    {
        options.RegisterTypeAlias(typeof(ScriptPreparationException), nameof(ScriptPreparationException));
        options.RegisterTypeAlias(typeof(JavaScriptException), nameof(JavaScriptException));
        options.RegisterTypeAlias(typeof(SyntaxErrorException), nameof(SyntaxErrorException));

        var wrapperExceptionType = typeof(JavaScriptException).GetNestedType("JavaScriptErrorWrapperException", BindingFlags.Public | BindingFlags.NonPublic);
        if (wrapperExceptionType != null)
            options.RegisterTypeAlias(wrapperExceptionType, "Jint.JavaScriptErrorWrapperException");
    }
}

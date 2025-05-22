using System.Diagnostics.CodeAnalysis;
using Jint;
using Jint.Native;
using Jint.Runtime.Interop;

namespace Elsa.Expressions.JavaScript.ObjectConverters;

internal class EnumToStringConverter : IObjectConverter
{
    public bool TryConvert(Engine engine, object value, [NotNullWhen(true)] out JsValue? result)
    {
        if (value is Enum)
        {
            result = value.ToString();
            return true;
        }

        result = JsValue.Null;
        return false;
    }
}
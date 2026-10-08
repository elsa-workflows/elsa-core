using System.Diagnostics.CodeAnalysis;
using Jint;
using Jint.Native;
using Jint.Runtime.Interop;

namespace Elsa.Expressions.JavaScript.ObjectConverters;

/// <summary>
/// Converts a byte array to a <see cref="JsValue"/> instance representing a Uint8Array.
/// </summary>
internal class ByteArrayConverter : IObjectConverter
{
    public bool TryConvert(Engine engine, object value, [NotNullWhen(true)] out JsValue? result)
    {
        if (value is byte[] bytes)
        {
            result = engine.Intrinsics.ArrayBuffer.Construct(bytes);
            return true;
        }

        result = JsValue.Null;
        return false;
    }
}
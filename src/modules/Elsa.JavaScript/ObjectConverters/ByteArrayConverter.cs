using System.Diagnostics.CodeAnalysis;
using Jint;
using Jint.Native;
using Jint.Runtime.Interop;

namespace Elsa.JavaScript.ObjectConverters;

/// <summary>
/// Converts a byte array to a <see cref="JsValue"/> instance representing a Uint8Array.
/// </summary>
internal class ByteArrayConverter : IObjectConverter
{
    public bool TryConvert(Engine engine, object value, [NotNullWhen(true)] out JsValue? result)
    {
        if (value is byte[] bytes)
        {
            // TODO: Temporary: Uint8Array creates a copy of the byte array. Instead, we want to create a view or a buffer referencing the byte array.
            // See also: https://github.com/sebastienros/jint/pull/1590
            result = engine.Intrinsics.Uint8Array.Construct(bytes);
            return true;
        }

        result = JsValue.Null;
        return false;
    }
}
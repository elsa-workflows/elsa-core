using Jint;
using Jint.Native;
using Jint.Runtime.Interop;

namespace Elsa.Scripting.JavaScript.Converters.Jint
{
    /// <summary>
    /// Prevents Jint from returning byte[] as object[].
    /// </summary>
    internal class ByteArrayConverter : IObjectConverter
    {
        public bool TryConvert(Engine engine, object value, out JsValue result)
        {
            if (value is byte[] bytes)
            {
                var buffer = engine.Intrinsics.ArrayBuffer.Construct(bytes);
                result = buffer;
                return true;
            }

            result = JsValue.Null;
            return false;
        }
    }
}
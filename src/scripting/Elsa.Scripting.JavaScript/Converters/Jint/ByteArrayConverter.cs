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
            result = JsValue.Null;
            
            if (value is not byte[] buffer)
                return false;

            result = new ObjectWrapper(engine, buffer);
            
            return true;
        }
    }
}
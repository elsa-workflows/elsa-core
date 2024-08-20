using System.Dynamic;
using Elsa.JavaScript.Helpers;
using Jint;
using Jint.Native;
using Jint.Runtime.Interop;

namespace Elsa.JavaScript.ObjectConverters;

internal class ExpandoObjectConverter : IObjectConverter
{
    public bool TryConvert(Engine engine, object value, out JsValue result)
    {
        if (value is ExpandoObject expandoObject)
        {
            result = ConverterHelper.ConvertToJsObject(engine, expandoObject);
            return true;
        }

        result = JsValue.Null;
        return false;
    }
}
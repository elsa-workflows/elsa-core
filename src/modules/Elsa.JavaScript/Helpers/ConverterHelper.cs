using System.Collections;
using Elsa.Extensions;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;

namespace Elsa.JavaScript.Helpers;

internal static class ConverterHelper
{
    public static ObjectInstance ConvertToJsObject(Engine engine, IDictionary<string, object?> expando)
    {
        var jsObject = engine.Intrinsics.Object.Construct([]);

        foreach (var kvp in expando)
        {
            var value = kvp.Value;
            var jsValue = ConvertToJsValue(engine, value);
            var propertyDescriptor = new PropertyDescriptor(jsValue, true, true, true);
            jsObject.DefineOwnProperty(kvp.Key, propertyDescriptor);
        }

        return jsObject;
    }

    private static JsValue ConvertToJsValue(Engine engine, object? value)
    {
        if (value == null)
            return JsValue.Null;

        if (value is IDictionary<string, object?> dict)
            return ConvertToJsObject(engine, dict);

        var valueType = value.GetType();
        if (valueType.IsCollectionType())
        {
            var list = (ICollection)value;
            var jsArray = engine.Intrinsics.Array.Construct(list.Count);
            var index = 0;

            foreach (var item in list)
                jsArray.Set(index++, ConvertToJsValue(engine, item), true);

            return jsArray;
        }

        if (value is string str)
            return JsValue.FromObject(engine, str);

        if (value is int or double or float or decimal)
            return JsValue.FromObject(engine, Convert.ToDouble(value));

        if (value is bool b)
            return JsValue.FromObject(engine, b);

        return JsValue.FromObject(engine, value);
    }
}
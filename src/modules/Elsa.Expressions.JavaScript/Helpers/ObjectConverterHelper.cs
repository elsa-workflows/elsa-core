using System.Collections;
using System.Dynamic;
using Elsa.Extensions;
using Jint;
using Jint.Native;
using Jint.Native.Object;

namespace Elsa.Expressions.JavaScript.Helpers;

internal static class ObjectConverterHelper
{
    public static object? ProcessVariableValue(Engine engine, object? variableValue)
    {
        if (variableValue == null)
            return null;

        if (variableValue is not ExpandoObject expandoObject)
            return variableValue;

        return ConvertToJsObject(engine, expandoObject);
    }
    
    public static ObjectInstance ConvertToJsObject(Engine engine, IDictionary<string, object?> expando)
    {
        // CreateFromEntries defines the same writable, enumerable and configurable properties the explicit
        // descriptor used to spell out, but builds the object directly in the engine's hidden-class
        // representation rather than filling a per-object property dictionary. Two wins, both within a single
        // evaluation: it drops the second descriptor allocation the explicit one caused inside Jint's
        // ValidateAndApplyPropertyDescriptor, and objects presenting the same keys — sibling variables, and the
        // nested objects of a repeated payload shape — share one layout instead of each carrying its own
        // descriptors. Nothing carries across evaluations: hidden classes are interned per engine and Elsa
        // builds a fresh engine every time.
        return JsObject.CreateFromEntries(engine, expando.Select(kvp => new KeyValuePair<string, JsValue>(kvp.Key, ConvertToJsValue(engine, kvp.Value))));
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
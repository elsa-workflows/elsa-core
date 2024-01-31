using System.Collections.Generic;
using System.Dynamic;
using Jint;
using Jint.Runtime.Interop;
using Xunit;

namespace Elsa.IntegrationTests.Scenarios.JavaScriptListAndArray;

/// <summary>
/// Contains test cases for the functionality of the Engine class to enable list and array-like objects to be used in JavaScript as arrays.
/// </summary>
public class Tests
{
    private readonly Engine _engine;

    public Tests()
    {
        _engine = new Engine(cfg => cfg
            .SetWrapObjectHandler((engine, target, type) =>
            {
                var instance = new ObjectWrapper(engine, target);
                if (instance.IsArrayLike)
                {
                    instance.SetPrototypeOf(engine.Realm.Intrinsics.Array.PrototypeObject);
                }

                return instance;
            })
        );
    }

    [Fact(DisplayName = "Can access list properties as arrays")]
    public void Test1()
    {
        var person = new ExpandoObject();

        person.TryAdd("name", "John");
        person.TryAdd("age", 12);

        var languages = new List<object>
        {
            "English",
            "French"
        };

        person.TryAdd("languages", languages);

        var obj = new ExpandoObject();
        obj.TryAdd("persons", new List<object> { person });
        _engine.SetValue("o", obj);

        var name = _engine.Evaluate("o.persons.filter(x => x.age == 12)[0].name").ToString();
        var language = _engine.Evaluate("o.persons[0].languages.filter(x => x == 'English')[0]").ToString();
        Assert.Equal("John", name);
        Assert.Equal("English", language);
    }
}
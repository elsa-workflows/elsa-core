using System.Collections;
using System.Dynamic;
using System.Reflection;
using Elsa.Logging.Helpers;

namespace Elsa.Logging.Core.UnitTests
{
    public class LogArgumentHelperTests
    {
        [Fact]
        public void Null_Returns_Empty_Array()
        {
            var result = LogArgumentHelper.ToArgumentsArray(null);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ObjectArray_PassedThrough_AsIs()
        {
            object[] input =
            {
                1, "two", 3.0
            };
            var result = LogArgumentHelper.ToArgumentsArray(input);
            Assert.Same(input, result); // ensure it's not copied
        }

        [Fact]
        public void NonGeneric_IDictionary_To_KeyValuePairs()
        {
            var ht = new Hashtable
            {
                ["Name"] = "Alice",
                ["Age"] = 30
            };

            var result = LogArgumentHelper.ToArgumentsArray(ht);
            var dict = PairsToDictionary(result);

            Assert.Equal(2, dict.Count);
            Assert.Equal("Alice", dict["Name"]);
            Assert.Equal(30, dict["Age"]);
        }

        [Fact]
        public void Generic_IDictionary_To_KeyValuePairs()
        {
            IDictionary<string, object?> dictIn = new Dictionary<string, object?>
            {
                ["Name"] = "Bob",
                ["Active"] = true
            };

            var result = LogArgumentHelper.ToArgumentsArray(dictIn);
            var dict = PairsToDictionary(result);

            Assert.Equal(2, dict.Count);
            Assert.Equal("Bob", dict["Name"]);
            Assert.Equal(true, dict["Active"]);
        }

        [Fact]
        public void IReadOnlyDictionary_To_KeyValuePairs()
        {
            IReadOnlyDictionary<string, int> ro = new Dictionary<string, int>
            {
                ["A"] = 1,
                ["B"] = 2
            };

            var result = LogArgumentHelper.ToArgumentsArray(ro);
            var dict = PairsToDictionary(result);

            Assert.Equal(2, dict.Count);
            Assert.Equal(1, dict["A"]);
            Assert.Equal(2, dict["B"]);
        }

        [Fact]
        public void Enumerable_Of_KeyValuePair_To_ObjectKeyValuePairs()
        {
            var kvList = new List<KeyValuePair<string, object?>>
            {
                new("X", 42),
                new("Y", "why")
            };

            var result = LogArgumentHelper.ToArgumentsArray(kvList);
            var dict = PairsToDictionary(result);

            Assert.Equal(2, dict.Count);
            Assert.Equal(42, dict["X"]);
            Assert.Equal("why", dict["Y"]);
        }

        [Fact]
        public void Enumerable_To_ObjectArray()
        {
            var list = new List<object?>
            {
                1,
                "two",
                3.0
            };
            var result = LogArgumentHelper.ToArgumentsArray(list);

            Assert.Equal(3, result.Length);
            Assert.Equal(1, result[0]);
            Assert.Equal("two", result[1]);
            Assert.Equal(3.0, result[2]);
        }

        [Fact]
        public void Array_To_ObjectArray()
        {
            var arr = new[]
            {
                10, 20, 30
            };
            var result = LogArgumentHelper.ToArgumentsArray(arr);

            Assert.Equal(new object[]
            {
                10, 20, 30
            }, result);
        }

        [Fact]
        public void String_Is_Not_Treated_As_Enumerable()
        {
            // The helper intentionally excludes string from IEnumerable mapping.
            // It will therefore treat string as a single atomic value.
            var input = "hello";
            var result = LogArgumentHelper.ToArgumentsArray(input);

            // Expect a single-element array containing the string itself
            Assert.Single(result);
            Assert.Equal("hello", result[0]);
        }

        [Fact]
        public void ByteArray_Is_Not_Treated_As_Enumerable()
        {
            var input = new byte[]
            {
                1, 2, 3, 4
            };
            var result = LogArgumentHelper.ToArgumentsArray(input);

            // Expect property pairs (e.g., Length, LongLength, Rank, etc.)
            Assert.NotEmpty(result);
            var dict = PairsToDictionary(result);
            Assert.True(dict.ContainsKey("Length"));
            Assert.Equal(4, dict["Length"]);
        }

        private sealed class Poco
        {
            public string Name { get; set; } = default!;
            public int Age { get; set; }
            public bool IsActive { get; init; }
        }

        [Fact]
        public void PlainObject_To_PropertyPairs()
        {
            var poco = new Poco
            {
                Name = "Carol",
                Age = 27,
                IsActive = true
            };

            var result = LogArgumentHelper.ToArgumentsArray(poco);
            var dict = PairsToDictionary(result);

            Assert.Equal(3, dict.Count);
            Assert.Equal("Carol", dict["Name"]);
            Assert.Equal(27, dict["Age"]);
            Assert.Equal(true, dict["IsActive"]);
        }

        [Fact]
        public void AnonymousType_To_PropertyPairs()
        {
            var anon = new
            {
                First = "Dave",
                Score = 99,
                Flag = false
            };

            var result = LogArgumentHelper.ToArgumentsArray(anon);
            var dict = PairsToDictionary(result);

            Assert.Equal(3, dict.Count);
            Assert.Equal("Dave", dict["First"]);
            Assert.Equal(99, dict["Score"]);
            Assert.Equal(false, dict["Flag"]);
        }

        [Fact]
        public void ExpandoObject_As_Dictionary_To_KeyValuePairs()
        {
            dynamic expando = new ExpandoObject();
            expando.City = "Utrecht";
            expando.Zip = "3511";
            expando.Pop = 360000;

            var result = LogArgumentHelper.ToArgumentsArray((object)expando);
            var dict = PairsToDictionary(result);

            Assert.Equal(3, dict.Count);
            Assert.Equal("Utrecht", dict["City"]);
            Assert.Equal("3511", dict["Zip"]);
            Assert.Equal(360000, dict["Pop"]);
        }

        [Fact]
        public void MixedKeyTypes_In_Dictionary_Are_Supported()
        {
            var ht = new Hashtable
            {
                [1] = "one",
                ["two"] = 2
            };

            var result = LogArgumentHelper.ToArgumentsArray(ht);
            var dict = PairsToDictionary(result);

            Assert.Equal("one", dict[1]);
            Assert.Equal(2, dict["two"]);
        }

        [Fact]
        public void Enumerable_Of_ComplexItems_To_ObjectArray()
        {
            var list = new List<Poco>
            {
                new()
                {
                    Name = "A",
                    Age = 1,
                    IsActive = true
                },
                new()
                {
                    Name = "B",
                    Age = 2,
                    IsActive = false
                },
            };

            var result = LogArgumentHelper.ToArgumentsArray(list);
            Assert.Equal(2, result.Length);
            Assert.IsType<Poco>(result[0]);
            Assert.IsType<Poco>(result[1]);

            var p0 = (Poco)result[0];
            var p1 = (Poco)result[1];

            Assert.Equal("A", p0.Name);
            Assert.Equal("B", p1.Name);
        }

        private static IDictionary<object, object?> PairsToDictionary(object[] pairs)
        {
            var dict = new Dictionary<object, object?>();

            foreach (var item in pairs)
            {
                var t = item.GetType();
                var isKvp = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
                Assert.True(isKvp, $"Element is not a KeyValuePair<,> but was {t}");

                var key = t.GetProperty("Key", BindingFlags.Public | BindingFlags.Instance)!.GetValue(item);
                var value = t.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)!.GetValue(item);

                // Keys may collide in some collections; last write wins here for simplicity
                if (key != null)
                {
                    dict[key] = value;
                }
            }

            return dict;
        }
    }
}
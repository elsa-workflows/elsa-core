using System.Collections.ObjectModel;
using System.Dynamic;
using Elsa.Extensions;

namespace Elsa.Expressions.Options;

/// <summary>
/// Options for the expression feature.
/// </summary>
public class ExpressionOptions
{
    private readonly IDictionary<string, Type> _aliasTypeDictionary = new Dictionary<string, Type>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionOptions"/> class.
    /// </summary>
    public ExpressionOptions()
    {
        AliasTypeDictionary = new ReadOnlyDictionary<string, Type>(_aliasTypeDictionary);

        this.AddTypeAlias<short>("Int16");
        this.AddTypeAlias<int>("Int32");
        this.AddTypeAlias<long>("Int64");
        this.AddTypeAlias<long>("Long");
        this.AddTypeAlias<float>("Single");
        this.AddTypeAlias<object>("Object");
        this.AddTypeAlias<string>("String");
        this.AddTypeAlias<bool>("Boolean");
        this.AddTypeAlias<decimal>("Decimal");
        this.AddTypeAlias<double>("Double");
        this.AddTypeAlias<Guid>("Guid");
        this.AddTypeAlias<DateTime>("DateTime");
        this.AddTypeAlias<DateTimeOffset>("DateTimeOffset");
        this.AddTypeAlias<TimeSpan>("TimeSpan");
        this.AddTypeAlias<ExpandoObject>("ExpandoObject");
        this.AddTypeAlias<ExpandoObject>("JSON");
        this.AddTypeAlias<IDictionary<string, string>>("StringDictionary");
        this.AddTypeAlias<IDictionary<string, string>>("StringMap");
        this.AddTypeAlias<IDictionary<string, object>>("ObjectMap");
        this.AddTypeAlias<Dictionary<string, object>>("ObjectMap1");
        this.AddTypeAlias<IDictionary<string, object>>("ObjectDictionary");
    }

    /// <summary>
    /// Gets the type alias dictionary.
    /// </summary>
    public IDictionary<string, Type> AliasTypeDictionary { get; set; }

    /// <summary>
    /// Registers a well known type alias.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="alias">The alias.</param>
    /// <returns>The options.</returns>
    public ExpressionOptions RegisterTypeAlias(Type type, string alias)
    {
        _aliasTypeDictionary[alias] = type;
        return this;
    }
}
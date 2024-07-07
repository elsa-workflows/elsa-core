using System.Collections.ObjectModel;
using System.Dynamic;
using Elsa.Extensions;

namespace Elsa.Expressions.Options;

/// Options for the expression feature.
public class ExpressionOptions
{
    private readonly IDictionary<string, Type> _aliasTypeDictionary = new Dictionary<string, Type>();
    
    /// Initializes a new instance of the <see cref="ExpressionOptions"/> class.
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
        this.AddTypeAlias<Dictionary<string, string>>("StringDictionary");
        this.AddTypeAlias<Dictionary<string, string>>("StringMap");
        this.AddTypeAlias<Dictionary<string, object>>("ObjectMap");
        this.AddTypeAlias<Dictionary<string, object>>("ObjectDictionary");
    }
    
    /// Gets the type alias dictionary.
    public IDictionary<string, Type> AliasTypeDictionary { get; set; }
    
    /// Registers a well-known type alias.
    public ExpressionOptions RegisterTypeAlias(Type type, string alias)
    {
        _aliasTypeDictionary[alias] = type;
        return this;
    }
}
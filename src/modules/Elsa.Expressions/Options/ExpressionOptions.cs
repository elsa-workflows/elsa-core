using System.Collections.ObjectModel;
using System.Dynamic;
using Elsa.Extensions;

namespace Elsa.Expressions.Options;

/// <summary>
/// Options for the expression feature.
/// </summary>
public class ExpressionOptions
{
    private readonly IDictionary<Type, Type> _expressionHandlers = new Dictionary<Type, Type>();
    private readonly IDictionary<string, Type> _aliasTypeDictionary = new Dictionary<string, Type>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionOptions"/> class.
    /// </summary>
    public ExpressionOptions()
    {
        ExpressionHandlers = new ReadOnlyDictionary<Type, Type>(_expressionHandlers);
        AliasTypeDictionary = new ReadOnlyDictionary<string, Type>(_aliasTypeDictionary);
        
        this.AddTypeAlias<object>("Object");
        this.AddTypeAlias<string>("String");
        this.AddTypeAlias<bool>("Boolean");
        this.AddTypeAlias<short>("Int16");
        this.AddTypeAlias<int>("Int32");
        this.AddTypeAlias<long>("Int64");
        this.AddTypeAlias<decimal>("Decimal");
        this.AddTypeAlias<float>("Single");
        this.AddTypeAlias<double>("Double");
        this.AddTypeAlias<Guid>("Guid");
        this.AddTypeAlias<DateTime>("DateTime");
        this.AddTypeAlias<DateTimeOffset>("DateTimeOffset");
        this.AddTypeAlias<TimeSpan>("TimeSpan");
        this.AddTypeAlias<DateOnly>("DateOnly");
        this.AddTypeAlias<TimeOnly>("TimeOnly");
        this.AddTypeAlias<ExpandoObject>("ExpandoObject");
        this.AddTypeAlias<ExpandoObject>("JSON"); // Alias for ExpandoObject.
        this.AddTypeAlias<IDictionary<string, string>>("StringDictionary");
        this.AddTypeAlias<IDictionary<string, object>>("ObjectDictionary");
    }

    /// <summary>
    /// Gets the expression handlers.
    /// </summary>
    public IDictionary<Type, Type> ExpressionHandlers { get; }
    
    /// <summary>
    /// Gets the type alias dictionary.
    /// </summary>
    public IDictionary<string, Type> AliasTypeDictionary { get; set; }

    /// <summary>
    /// Registers an expression handler.
    /// </summary>
    /// <param name="expression">The expression type.</param>
    /// <param name="handler">The handler type.</param>
    /// <returns>The options.</returns>
    public ExpressionOptions RegisterExpressionHandler(Type expression, Type handler)
    {
        _expressionHandlers.Add(expression, handler);
        return this;
    }

    /// <summary>
    /// Registers an expression handler.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <typeparam name="TExpression">The expression type.</typeparam>
    /// <returns>The options.</returns>
    public ExpressionOptions RegisterExpressionHandler<THandler, TExpression>() => RegisterExpressionHandler<THandler>(typeof(TExpression));
        
    /// <summary>
    /// Registers an expression handler.
    /// </summary>
    /// <param name="expression">The expression type.</param>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <returns>The options.</returns>
    public ExpressionOptions RegisterExpressionHandler<THandler>(Type expression)
    {
        _expressionHandlers.Add(expression, typeof(THandler));
        return this;
    }
    
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
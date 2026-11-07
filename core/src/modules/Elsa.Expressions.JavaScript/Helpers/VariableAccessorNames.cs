using Humanizer;

namespace Elsa.Expressions.JavaScript.Helpers;

/// <summary>
/// Decides which variable each generated <c>get{Name}</c>/<c>set{Name}</c> accessor pair belongs to.
/// </summary>
internal static class VariableAccessorNames
{
    /// <summary>
    /// Maps each accessor suffix (the Pascalized variable name) to the variable whose accessors it names.
    /// </summary>
    /// <remarks>
    /// <c>Pascalize()</c> drops underscores, so <c>meting_context</c> and <c>metingContext</c> both become <c>MetingContext</c>,
    /// and <c>_foo</c> and <c>foo</c> both become <c>Foo</c>. Names made of letters and digits are mapped first, the last one
    /// winning as before. A name containing <c>_</c> or <c>$</c> only gets a suffix that is still free, so an existing accessor
    /// never changes variable. A variable left without accessors is still reached through <c>variables.&lt;name&gt;</c> or
    /// <c>getVariable("&lt;name&gt;")</c>.
    /// </remarks>
    public static IReadOnlyDictionary<string, T> Resolve<T>(IEnumerable<T> variables, Func<T, string> getName)
    {
        var validVariables = variables.Where(x => VariableNameValidator.IsValidVariableName(getName(x))).ToList();
        var accessors = new Dictionary<string, T>(StringComparer.Ordinal);

        foreach (var variable in validVariables.Where(x => IsLettersAndDigits(getName(x))))
            accessors[getName(variable).Pascalize()] = variable;

        foreach (var variable in validVariables.Where(x => !IsLettersAndDigits(getName(x))))
            accessors.TryAdd(getName(variable).Pascalize(), variable);

        return accessors;
    }

    private static bool IsLettersAndDigits(string name) => name.All(char.IsLetterOrDigit);
}

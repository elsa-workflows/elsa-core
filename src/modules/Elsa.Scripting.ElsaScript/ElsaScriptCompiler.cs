using Elsa.Scripting.ElsaScript.Lowering;
using Elsa.Scripting.ElsaScript.Parsing;

namespace Elsa.Scripting.ElsaScript;

public class ElsaScriptCompiler
{
    private readonly ElsaScriptParser _parser = new();
    private readonly Lowerer _lowerer = new();

    public object Compile(string source)
    {
        var program = _parser.Parse(source);
        return _lowerer.Lower(program);
    }
}

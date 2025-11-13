using System.Threading.Tasks;
using Elsa.Scripting.ElsaScript.Parsers;
using Elsa.Scripting.ElsaScript.Lowering;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows;

public static class ElsaScriptQuickTest
{
    public static async Task<Elsa.Workflows.IActivity> BuildAsync(Elsa.Workflows.IServiceProvider sp)
    {
        var script = @"WriteLine(`Hello, ${name}`); if (=> true) WriteLine(""OK"");";
        var program = ProgramParser.Instance.Parse(script);
        var compiler = new Compiler(sp.GetRequiredService<IActivityRegistryLookupService>());
        return await compiler.CompileAsync(program);
    }
}


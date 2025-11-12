using System.Collections.Generic;
using System.Linq;
using Elsa.Scripting.ElsaScript;
using Elsa.Scripting.ElsaScript.Lowering;
using Xunit;

namespace Elsa.Scripting.ElsaScript.IntegrationTests;

public class ElsaScriptCompilerTests
{
    private readonly ElsaScriptCompiler _compiler = new();

    [Fact]
    public void CompilesForLoopIntoStubModel()
    {
        const string script = @"
use expressions js;
for (let i = 0; => i < 3; => i = i + 1) {
  WriteLine(Text: \"Hello\");
}
";

        var workflow = Assert.IsType<WfSequence>(_compiler.Compile(script));
        var forActivity = Assert.IsType<WfActivity>(workflow.Activities.Single());
        Assert.Equal("For", forActivity.Type);

        var initializer = Assert.IsType<dynamic>(forActivity.Inputs["Initializer"]!);
        Assert.Equal("let", (string)initializer.Kind);
        Assert.Equal("i", (string)initializer.Name);
        Assert.NotNull(initializer.Initializer);

        var body = Assert.IsType<WfSequence>(forActivity.Inputs["Body"]);
        var writeLine = Assert.IsType<WfActivity>(body.Activities.Single());
        Assert.Equal("WriteLine", writeLine.Type);
        Assert.True(((bool?)writeLine.CanStart) is null or false);
    }

    [Fact]
    public void CompilesWhileLoop()
    {
        const string script = @"
use expressions js;
while (=> shouldContinue()) {
  Log(=> \"loop\");
}
";

        var workflow = Assert.IsType<WfSequence>(_compiler.Compile(script));
        var whileActivity = Assert.IsType<WfActivity>(workflow.Activities.Single());
        Assert.Equal("While", whileActivity.Type);
        var body = Assert.IsType<WfSequence>(whileActivity.Inputs["Body"]);
        Assert.Single(body.Activities);
    }

    [Fact]
    public void CompilesSwitchStatement()
    {
        const string script = @"
switch (=> status) {
  case \"Ok\": {
    WriteLine(\"OK\");
  }
  case \"Error\": {
    WriteLine(\"ERR\");
  }
  default: {
    WriteLine(\"???\");
  }
}
";

        var workflow = Assert.IsType<WfSequence>(_compiler.Compile(script));
        var switchActivity = Assert.IsType<WfActivity>(workflow.Activities.Single());
        Assert.Equal("Switch", switchActivity.Type);

        var cases = Assert.IsAssignableFrom<IEnumerable<WfSwitchCase>>(switchActivity.Inputs["Cases"]!);
        Assert.Equal(2, cases.Count());
        Assert.NotNull(switchActivity.Inputs["Default"]);
    }
}

using Elsa.Workflows.Activities;

namespace Elsa.Scripting.ElsaScript.IntegrationTests;

public class ElsaScriptCompilerTests
{
    private readonly Lowering.ElsaScriptCompiler _compiler = new();

    [Fact]
    public void CompilesForLoopIntoWorkflow()
    {
        const string script = @"
use expressions js;
for (let i = 0; => i < 3; => i = i + 1) {
  WriteLine(Text: ""Hello"");
}
";

        var workflow = _compiler.Compile(script);
        Assert.NotNull(workflow);
        Assert.IsType<Workflow>(workflow);

        // Root should be a Sequence
        var rootSequence = Assert.IsType<Sequence>(workflow.Root);
        Assert.Single(rootSequence.Activities);

        // For loop is compiled as While with metadata
        var whileActivity = Assert.IsType<While>(rootSequence.Activities.Single());
        Assert.NotNull(whileActivity.Condition);
        Assert.NotNull(whileActivity.Body);

        // Check metadata
        Assert.True(whileActivity.CustomProperties.ContainsKey("ElsaScript.Type"));
        Assert.Equal("for", whileActivity.CustomProperties["ElsaScript.Type"]);

        // Check body contains WriteLine
        var bodySequence = Assert.IsType<Sequence>(whileActivity.Body);
        Assert.Single(bodySequence.Activities);
        Assert.IsType<WriteLine>(bodySequence.Activities.Single());
    }

    [Fact]
    public void CompilesWhileLoop()
    {
        const string script = @"
use expressions js;
while (=> shouldContinue()) {
  Log(=> ""loop"");
}
";

        var workflow = _compiler.Compile(script);
        Assert.NotNull(workflow);
        Assert.IsType<Workflow>(workflow);

        var rootSequence = Assert.IsType<Sequence>(workflow.Root);
        var whileActivity = Assert.IsType<While>(rootSequence.Activities.Single());
        Assert.NotNull(whileActivity.Condition);

        var body = Assert.IsType<Sequence>(whileActivity.Body);
        Assert.Single(body.Activities);

        // Log is a generic activity
        var logActivity = body.Activities.Single();
        Assert.Equal("Log", logActivity.Type);
    }

    [Fact]
    public void CompilesSwitchStatement()
    {
        const string script = @"
switch (=> status) {
  case ""Ok"": {
    WriteLine(""OK"");
  }
  case ""Error"": {
    WriteLine(""ERR"");
  }
  default: {
    WriteLine(""???"");
  }
}
";

        var workflow = _compiler.Compile(script);
        Assert.NotNull(workflow);
        Assert.IsType<Workflow>(workflow);

        var rootSequence = Assert.IsType<Sequence>(workflow.Root);
        var switchActivity = Assert.IsType<Switch>(rootSequence.Activities.Single());

        Assert.Equal(2, switchActivity.Cases.Count);
        Assert.NotNull(switchActivity.Default);

        // Check that each case has a body
        foreach (var switchCase in switchActivity.Cases)
        {
            Assert.NotNull(switchCase.Activity);
            var caseBody = Assert.IsType<Sequence>(switchCase.Activity);
            Assert.Single(caseBody.Activities);
            Assert.IsType<WriteLine>(caseBody.Activities.Single());
        }

        // Check default
        var defaultBody = Assert.IsType<Sequence>(switchActivity.Default);
        Assert.Single(defaultBody.Activities);
        Assert.IsType<WriteLine>(defaultBody.Activities.Single());
    }
}

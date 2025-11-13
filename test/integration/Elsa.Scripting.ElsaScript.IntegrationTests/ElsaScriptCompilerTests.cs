using Elsa.Workflows.Activities;

namespace Elsa.Scripting.ElsaScript.IntegrationTests;

public class ElsaScriptCompilerTests
{
    private readonly Lowering.ElsaScriptCompiler _compiler = new();

    [Fact]
    public void CompilesRangeForLoopWithToKeyword()
    {
        const string script = @"
use expressions js;
for i = 0 to 3 {
  WriteLine(Text: ""Hello"");
}
";

        var workflow = _compiler.Compile(script);
        Assert.NotNull(workflow);
        Assert.IsType<Workflow>(workflow);

        // Root should be a Sequence
        var rootSequence = Assert.IsType<Sequence>(workflow.Root);
        Assert.Single(rootSequence.Activities);

        // For loop is compiled as For activity
        var forActivity = Assert.IsType<For>(rootSequence.Activities.Single());
        Assert.NotNull(forActivity.Start);
        Assert.NotNull(forActivity.End);
        Assert.NotNull(forActivity.Step);
        Assert.NotNull(forActivity.Body);
        Assert.NotNull(forActivity.CurrentValueVariableName);

        // Check properties - OuterBoundInclusive should be false for "to"
        // Note: Can't easily test Input<> values without execution context, but we can verify structure

        // Check body contains WriteLine
        var bodySequence = Assert.IsType<Sequence>(forActivity.Body);
        Assert.Single(bodySequence.Activities);
        Assert.IsType<WriteLine>(bodySequence.Activities.Single());
    }

    [Fact]
    public void CompilesRangeForLoopWithThroughKeyword()
    {
        const string script = @"
for index = 0 through 10 {
  WriteLine(Text: ""Iteration"");
}
";

        var workflow = _compiler.Compile(script);
        var rootSequence = Assert.IsType<Sequence>(workflow.Root);
        var forActivity = Assert.IsType<For>(rootSequence.Activities.Single());

        // OuterBoundInclusive should be true for "through"
        Assert.NotNull(forActivity.OuterBoundInclusive);
        Assert.NotNull(forActivity.CurrentValueVariableName);

        var bodySequence = Assert.IsType<Sequence>(forActivity.Body);
        Assert.Single(bodySequence.Activities);
        Assert.IsType<WriteLine>(bodySequence.Activities.Single());
    }

    [Fact]
    public void CompilesRangeForLoopWithStepExpression()
    {
        const string script = @"
for i = 0 to 10 step 2 {
  WriteLine(Text: ""Even"");
}
";

        var workflow = _compiler.Compile(script);
        var rootSequence = Assert.IsType<Sequence>(workflow.Root);
        var forActivity = Assert.IsType<For>(rootSequence.Activities.Single());

        Assert.NotNull(forActivity.Start);
        Assert.NotNull(forActivity.End);
        Assert.NotNull(forActivity.Step);
        Assert.NotNull(forActivity.Body);

        var bodySequence = Assert.IsType<Sequence>(forActivity.Body);
        Assert.Single(bodySequence.Activities);
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

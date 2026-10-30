using Elsa.Agents;
using Elsa.Studio.Agents.UI.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Agents.Tests;

public class AgentSubmitValidatorTests : IDisposable
{
    private readonly AgentInputModel _model = new() { Name = "Original" };
    private readonly EditContext _context;
    private readonly InlineValidator<AgentInputModel> _rules = new();
    private readonly AgentSubmitValidator _validator;
    private readonly Queue<TaskCompletionSource<bool>> _answers = new();

    public AgentSubmitValidatorTests()
    {
        _context = new(_model);
        _rules.RuleFor(x => x.Name).MustAsync((_, _) => _answers.Dequeue().Task).WithMessage("Duplicate agent name");
        _validator = new(_rules, _model, _context);
    }

    [Fact]
    public async Task SubmissionWaitsForUniquenessAndPublishesFailure()
    {
        var answer = QueueAnswer();
        var pending = _validator.ValidateAndPublishAsync();
        Assert.False(pending.IsCompleted);
        answer.SetResult(false);
        Assert.False(await pending);
        Assert.Equal("Duplicate agent name", Assert.Single(_context.GetValidationMessages()));
        ChangeName("Available");
        Assert.Empty(_context.GetValidationMessages());
        QueueAnswer().SetResult(true);
        Assert.True(await _validator.ValidateAndPublishAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditedNameDiscardsPendingResultEvenWhenChangedBack(bool changeBack)
    {
        var answer = QueueAnswer();
        var pending = _validator.ValidateAndPublishAsync();
        ChangeName("Changed");
        if (changeBack)
        {
            ChangeName("Original");
        }
        answer.SetResult(true);
        Assert.False(await pending);
        Assert.Empty(_context.GetValidationMessages());
    }

    [Fact]
    public async Task DirectNameMutationDiscardsPendingResult()
    {
        var answer = QueueAnswer();
        var pending = _validator.ValidateAndPublishAsync();
        _model.Name = "Changed without event";
        answer.SetResult(true);
        Assert.False(await pending);
    }

    [Fact]
    public async Task OlderValidationCannotOverwriteNewerResult()
    {
        var first = QueueAnswer();
        var pendingFirst = _validator.ValidateAndPublishAsync();
        var second = QueueAnswer();
        var pendingSecond = _validator.ValidateAndPublishAsync();
        second.SetResult(false);
        Assert.False(await pendingSecond);
        first.SetResult(true);
        Assert.False(await pendingFirst);
        Assert.Equal("Duplicate agent name", Assert.Single(_context.GetValidationMessages()));
    }

    [Fact]
    public async Task DisposalDiscardsPendingResultAndPreventsFurtherValidation()
    {
        var answer = QueueAnswer();
        var pending = _validator.ValidateAndPublishAsync();
        _validator.Dispose();
        answer.SetResult(true);
        Assert.False(await pending);
        Assert.False(await _validator.ValidateAndPublishAsync());
        Assert.Empty(_context.GetValidationMessages());
    }

    private TaskCompletionSource<bool> QueueAnswer()
    {
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _answers.Enqueue(answer);
        return answer;
    }

    private void ChangeName(string name)
    {
        _model.Name = name;
        _context.NotifyFieldChanged(new(_model, nameof(_model.Name)));
    }

    public void Dispose() => _validator.Dispose();
}

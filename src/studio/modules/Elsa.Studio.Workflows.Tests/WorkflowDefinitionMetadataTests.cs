using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Notifications;
using Elsa.Studio.Workflows.Tests.Support;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Refit;
using System.Reflection;
using Xunit;

using MetadataComponent = Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Metadata.Metadata;

namespace Elsa.Studio.Workflows.Tests;

public sealed class WorkflowDefinitionMetadataTests : BunitContext, IAsyncLifetime
{
    private readonly ControlledWorkflowDefinitionService _workflowDefinitionService = new();

    public WorkflowDefinitionMetadataTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IWorkflowDefinitionService>(_workflowDefinitionService);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task DescriptionEditSurvivesRerenderWhileNameBlurValidationIsPending()
    {
        var definition = CreateDefinition();
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(definition, callbackValues);
        var fields = cut.FindComponents<MudTextField<string>>();
        var nameInput = fields[0].Find("input");
        var descriptionInput = fields[1].Find("textarea");

        await descriptionInput.InputAsync("edited description");
        var validation = _workflowDefinitionService.EnqueueValidation();

        var blurTask = nameInput.BlurAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Render(parameters => parameters.Add(x => x.WorkflowDefinition, definition));
        validation.Result.SetResult(true);
        await blurTask;

        Assert.Equal("edited description", definition.Description);
        Assert.Equal("edited description", callbackValues.Single().Description);
        Assert.Equal("edited description", cut.FindComponents<MudTextField<string>>()[1].Find("textarea").GetAttribute("value"));
    }

    [Fact]
    public async Task NameEditSurvivesRerenderWhileDescriptionBlurValidationIsPending()
    {
        var definition = CreateDefinition();
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(definition, callbackValues);
        var fields = cut.FindComponents<MudTextField<string>>();
        var nameInput = fields[0].Find("input");
        var descriptionInput = fields[1].Find("textarea");

        await nameInput.InputAsync("edited name");
        var validation = _workflowDefinitionService.EnqueueValidation();

        var blurTask = descriptionInput.BlurAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Render(parameters => parameters.Add(x => x.WorkflowDefinition, definition));
        validation.Result.SetResult(true);
        await blurTask;

        Assert.Equal("edited name", definition.Name);
        Assert.Equal("edited name", callbackValues.Single().Name);
        Assert.Equal("edited name", cut.FindComponents<MudTextField<string>>()[0].Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task OverlappingBlurValidationsKeepTheLatestMetadataAfterOutOfOrderCompletion()
    {
        var definition = CreateDefinition();
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(definition, callbackValues);
        var fields = cut.FindComponents<MudTextField<string>>();
        var nameInput = fields[0].Find("input");
        var descriptionInput = fields[1].Find("textarea");

        await nameInput.InputAsync("edited name");
        await descriptionInput.InputAsync("edited description");
        var firstValidation = _workflowDefinitionService.EnqueueValidation();
        var secondValidation = _workflowDefinitionService.EnqueueValidation();

        var firstBlurTask = nameInput.BlurAsync();
        await firstValidation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var secondBlurTask = descriptionInput.BlurAsync();
        await secondValidation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        secondValidation.Result.SetResult(true);
        await secondBlurTask;
        firstValidation.Result.SetResult(false);
        await firstBlurTask;

        Assert.Equal("edited name", definition.Name);
        Assert.Equal("edited description", definition.Description);
        Assert.Equal("edited name", cut.FindComponents<MudTextField<string>>()[0].Find("input").GetAttribute("value"));
        Assert.Equal("edited description", cut.FindComponents<MudTextField<string>>()[1].Find("textarea").GetAttribute("value"));
        Assert.Single(callbackValues);
        Assert.Equal(("edited name", "edited description"), callbackValues[^1]);
        Assert.DoesNotContain("A workflow with this name already exists.", cut.Markup);
    }

    [Fact]
    public async Task StaleValidationDoesNotCommitANameEditedAfterValidationStarted()
    {
        var definition = CreateDefinition();
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(definition, callbackValues);
        var nameInput = cut.FindComponents<MudTextField<string>>()[0].Find("input");

        await nameInput.InputAsync("name A");
        var firstValidation = _workflowDefinitionService.EnqueueValidation();
        var firstBlurTask = nameInput.BlurAsync();
        await firstValidation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await nameInput.InputAsync("name B");
        var secondValidation = _workflowDefinitionService.EnqueueValidation();
        var secondBlurTask = nameInput.BlurAsync();
        await secondValidation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        firstValidation.Result.SetResult(false);
        await firstBlurTask;

        Assert.Equal("initial name", definition.Name);
        Assert.Equal("name A", firstValidation.Name);

        secondValidation.Result.SetResult(true);
        await secondBlurTask;

        Assert.Equal("name B", definition.Name);
        Assert.Equal("name B", callbackValues.Single().Name);
        Assert.Equal("name B", cut.FindComponents<MudTextField<string>>()[0].Find("input").GetAttribute("value"));
        Assert.Equal("name B", secondValidation.Name);
    }

    [Fact]
    public async Task PendingValidationCommitsToTheLatestSameVersionSaveResponse()
    {
        var initialDefinition = CreateDefinition();
        var saveResponse = new WorkflowDefinition
        {
            Id = initialDefinition.Id,
            DefinitionId = initialDefinition.DefinitionId,
            Name = "older server name",
            Description = "older server description"
        };
        var currentDefinition = initialDefinition;
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(initialDefinition, callbackValues, () => currentDefinition);
        var nameInput = cut.FindComponents<MudTextField<string>>()[0].Find("input");

        await nameInput.InputAsync("local name");
        var validation = _workflowDefinitionService.EnqueueValidation();
        var blurTask = nameInput.BlurAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        currentDefinition = saveResponse;
        cut.Render(parameters => parameters.Add(x => x.WorkflowDefinition, saveResponse));
        validation.Result.SetResult(true);
        await blurTask;

        Assert.Equal("initial name", initialDefinition.Name);
        Assert.Equal("local name", saveResponse.Name);
        Assert.Equal("local name", callbackValues.Single().Name);
    }

    [Fact]
    public async Task FormSubmissionCommitsTheCurrentMetadataAfterAsyncValidation()
    {
        var definition = CreateDefinition();
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(definition, callbackValues);
        var nameInput = cut.FindComponents<MudTextField<string>>()[0].Find("input");

        await nameInput.InputAsync("submitted name");
        var validation = _workflowDefinitionService.EnqueueValidation();
        var submitTask = cut.Find("form").SubmitAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        validation.Result.SetResult(true);
        await submitTask;

        Assert.Equal("submitted name", definition.Name);
        Assert.Equal("submitted name", callbackValues.Single().Name);
    }

    [Fact]
    public async Task FormSubmissionDoesNotCommitWhileValidationIsPendingOrWhenItFails()
    {
        var definition = CreateDefinition();
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(definition, callbackValues);
        var nameInput = cut.FindComponents<MudTextField<string>>()[0].Find("input");

        await nameInput.InputAsync("invalid name");
        var validation = _workflowDefinitionService.EnqueueValidation();
        var submitTask = cut.Find("form").SubmitAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("initial name", definition.Name);
        Assert.Empty(callbackValues);

        validation.Result.SetResult(false);
        await submitTask;

        Assert.Equal("initial name", definition.Name);
        Assert.Empty(callbackValues);
        Assert.Contains("A workflow with this name already exists.", cut.Markup);
    }

    [Fact]
    public async Task FormSubmissionDoesNotCommitAnEmptyName()
    {
        var definition = CreateDefinition();
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(definition, callbackValues);
        var nameInput = cut.FindComponents<MudTextField<string>>()[0].Find("input");

        await nameInput.InputAsync(string.Empty);
        var validation = _workflowDefinitionService.EnqueueValidation();
        var submitTask = cut.Find("form").SubmitAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        validation.Result.SetResult(true);
        await submitTask;

        Assert.Equal("initial name", definition.Name);
        Assert.Empty(callbackValues);
        Assert.Contains("Please enter a name for the workflow.", cut.Markup);
    }

    [Fact]
    public async Task AParentRerenderAfterACommitKeepsTheFormValidating()
    {
        var definition = CreateDefinition();
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(definition, callbackValues);

        await cut.FindComponents<MudTextField<string>>()[0].Find("input").InputAsync("committed name");
        var commit = _workflowDefinitionService.EnqueueValidation();
        var blurTask = cut.FindComponents<MudTextField<string>>()[0].Find("input").BlurAsync();
        await commit.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        commit.Result.SetResult(true);
        await blurTask;

        Assert.Single(callbackValues);

        // The parent rerenders this component while handling WorkflowDefinitionUpdated, which must not
        // disturb the form's EditContext or the validation wiring hanging off it.
        cut.Render(parameters => parameters.Add(x => x.WorkflowDefinition, definition));

        await cut.FindComponents<MudTextField<string>>()[0].Find("input").InputAsync(string.Empty);
        var rejected = _workflowDefinitionService.EnqueueValidation();
        var submitTask = cut.Find("form").SubmitAsync();
        await rejected.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        rejected.Result.SetResult(true);
        await submitTask;

        Assert.Equal("committed name", definition.Name);
        Assert.Single(callbackValues);
        Assert.Contains("Please enter a name for the workflow.", cut.Markup);
    }

    [Fact]
    public async Task FormSubmissionDoesNotCommitANameChangedWhileItsValidationIsPending()
    {
        var definition = CreateDefinition();
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(definition, callbackValues);
        var nameInput = cut.FindComponents<MudTextField<string>>()[0].Find("input");

        await nameInput.InputAsync("name A");
        var validation = _workflowDefinitionService.EnqueueValidation();
        var submitTask = cut.Find("form").SubmitAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await nameInput.InputAsync("name B");
        validation.Result.SetResult(true);
        await submitTask;

        Assert.Equal("initial name", definition.Name);
        Assert.Empty(callbackValues);
        Assert.Equal("name B", cut.FindComponents<MudTextField<string>>()[0].Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task FormSubmissionFromAnOldWorkflowVersionCannotCommitIntoTheReplacement()
    {
        var initialDefinition = CreateDefinition();
        var replacementDefinition = new WorkflowDefinition
        {
            Id = "version-2",
            DefinitionId = initialDefinition.DefinitionId,
            Name = "replacement name",
            Description = "replacement description"
        };
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(initialDefinition, callbackValues);
        var nameInput = cut.FindComponents<MudTextField<string>>()[0].Find("input");

        await nameInput.InputAsync("old version name");
        var validation = _workflowDefinitionService.EnqueueValidation();
        var submitTask = cut.Find("form").SubmitAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Render(parameters => parameters.Add(x => x.WorkflowDefinition, replacementDefinition));
        validation.Result.SetResult(true);
        await submitTask;

        Assert.Equal("initial name", initialDefinition.Name);
        Assert.Equal("replacement name", replacementDefinition.Name);
        Assert.Empty(callbackValues);
    }

    [Fact]
    public async Task ValidationFromAnOldWorkflowVersionCannotCommitIntoTheReplacement()
    {
        var initialDefinition = CreateDefinition();
        var replacementDefinition = new WorkflowDefinition
        {
            Id = "version-2",
            DefinitionId = initialDefinition.DefinitionId,
            Name = "replacement name",
            Description = "replacement description"
        };
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(initialDefinition, callbackValues);
        var nameInput = cut.FindComponents<MudTextField<string>>()[0].Find("input");

        await nameInput.InputAsync("edited old version");
        var validation = _workflowDefinitionService.EnqueueValidation();
        var blurTask = nameInput.BlurAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Render(parameters => parameters.Add(x => x.WorkflowDefinition, replacementDefinition));
        validation.Result.SetResult(true);
        await blurTask;

        Assert.Equal("initial name", initialDefinition.Name);
        Assert.Equal("replacement name", replacementDefinition.Name);
        Assert.Empty(callbackValues);
        Assert.Equal("replacement name", cut.FindComponents<MudTextField<string>>()[0].Find("input").GetAttribute("value"));
    }

    [Fact]
    public void AStaleSameVersionSaveResponseDoesNotOverwriteLocalMetadata()
    {
        var initialDefinition = CreateDefinition();
        var saveResponse = new WorkflowDefinition
        {
            Id = initialDefinition.Id,
            DefinitionId = initialDefinition.DefinitionId,
            Name = "older server name",
            Description = "older server description"
        };
        var cut = RenderMetadata(initialDefinition, []);
        var nameInput = cut.FindComponents<MudTextField<string>>()[0].Find("input");
        var descriptionInput = cut.FindComponents<MudTextField<string>>()[1].Find("textarea");

        nameInput.Input("local name");
        descriptionInput.Input("local description");
        cut.Render(parameters => parameters.Add(x => x.WorkflowDefinition, saveResponse));

        Assert.Equal("local name", cut.FindComponents<MudTextField<string>>()[0].Find("input").GetAttribute("value"));
        Assert.Equal("local description", cut.FindComponents<MudTextField<string>>()[1].Find("textarea").GetAttribute("value"));
    }

    [Fact]
    public async Task OlderSameVersionSaveResponseDoesNotOverwriteSubmittedLocalMetadata()
    {
        var initialDefinition = CreateDefinition();
        var saveResponse = new WorkflowDefinition
        {
            Id = initialDefinition.Id,
            DefinitionId = initialDefinition.DefinitionId,
            Name = "older server name",
            Description = "older server description"
        };
        var currentDefinition = initialDefinition;
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(initialDefinition, callbackValues, () => currentDefinition);
        var nameInput = cut.FindComponents<MudTextField<string>>()[0].Find("input");
        var descriptionInput = cut.FindComponents<MudTextField<string>>()[1].Find("textarea");

        await nameInput.InputAsync("submitted local name");
        await descriptionInput.InputAsync("submitted local description");
        var validation = _workflowDefinitionService.EnqueueValidation();
        var submitTask = cut.Find("form").SubmitAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        validation.Result.SetResult(true);
        await submitTask;

        Assert.Equal("submitted local name", initialDefinition.Name);
        Assert.Equal("submitted local description", initialDefinition.Description);

        currentDefinition = saveResponse;
        cut.Render(parameters => parameters.Add(x => x.WorkflowDefinition, saveResponse));

        Assert.Equal("submitted local name", cut.FindComponents<MudTextField<string>>()[0].Find("input").GetAttribute("value"));
        Assert.Equal("submitted local description", cut.FindComponents<MudTextField<string>>()[1].Find("textarea").GetAttribute("value"));
        Assert.Equal("submitted local name", saveResponse.Name);
        Assert.Equal("submitted local description", saveResponse.Description);
        Assert.Single(callbackValues);
    }

    [Fact]
    public async Task OlderSameVersionSaveResponseDoesNotCommitAnUnvalidatedEdit()
    {
        var initialDefinition = CreateDefinition();
        var saveResponse = new WorkflowDefinition
        {
            Id = initialDefinition.Id,
            DefinitionId = initialDefinition.DefinitionId,
            Name = "older server name",
            Description = "older server description"
        };
        var currentDefinition = initialDefinition;
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(initialDefinition, callbackValues, () => currentDefinition);
        var nameInput = cut.FindComponents<MudTextField<string>>()[0].Find("input");

        await nameInput.InputAsync("submitted name A");
        var validation = _workflowDefinitionService.EnqueueValidation();
        var submitTask = cut.Find("form").SubmitAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        validation.Result.SetResult(true);
        await submitTask;

        await nameInput.InputAsync("unvalidated name B");
        currentDefinition = saveResponse;
        cut.Render(parameters => parameters.Add(x => x.WorkflowDefinition, saveResponse));

        Assert.Equal("submitted name A", saveResponse.Name);
        Assert.Equal("unvalidated name B", cut.FindComponents<MudTextField<string>>()[0].Find("input").GetAttribute("value"));
        Assert.Single(callbackValues);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MatchingSaveResponseKeepsLaterUnvalidatedEditAndRejectsAnOlderResponse(bool editAfterAcknowledgement)
    {
        var initialDefinition = CreateDefinition();
        var matchingResponse = new WorkflowDefinition
        {
            Id = initialDefinition.Id,
            DefinitionId = initialDefinition.DefinitionId,
            Name = "submitted name A",
            Description = initialDefinition.Description
        };
        var olderResponse = new WorkflowDefinition
        {
            Id = initialDefinition.Id,
            DefinitionId = initialDefinition.DefinitionId,
            Name = "older server name",
            Description = "older server description"
        };
        var currentDefinition = initialDefinition;
        var callbackValues = new List<(string? Name, string? Description)>();
        var cut = RenderMetadata(initialDefinition, callbackValues, () => currentDefinition);
        var nameInput = cut.FindComponents<MudTextField<string>>()[0].Find("input");

        await nameInput.InputAsync("submitted name A");
        var validation = _workflowDefinitionService.EnqueueValidation();
        var submitTask = cut.Find("form").SubmitAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        validation.Result.SetResult(true);
        await submitTask;

        if (editAfterAcknowledgement)
            await nameInput.InputAsync("unvalidated name B");

        currentDefinition = matchingResponse;
        cut.Render(parameters => parameters.Add(x => x.WorkflowDefinition, matchingResponse));

        Assert.Equal("submitted name A", matchingResponse.Name);
        Assert.Equal(editAfterAcknowledgement ? "unvalidated name B" : "submitted name A",
            cut.FindComponents<MudTextField<string>>()[0].Find("input").GetAttribute("value"));

        currentDefinition = olderResponse;
        cut.Render(parameters => parameters.Add(x => x.WorkflowDefinition, olderResponse));

        Assert.Equal("submitted name A", olderResponse.Name);
        Assert.Equal(editAfterAcknowledgement ? "unvalidated name B" : "submitted name A",
            cut.FindComponents<MudTextField<string>>()[0].Find("input").GetAttribute("value"));
        Assert.Single(callbackValues);
    }

    [Fact]
    public void ReplacingTheWorkflowDefinitionReloadsTheMetadataModel()
    {
        var initialDefinition = CreateDefinition();
        var replacementDefinition = new WorkflowDefinition
        {
            Id = "version-2",
            DefinitionId = initialDefinition.DefinitionId,
            Name = "replacement name",
            Description = "replacement description"
        };
        var cut = RenderMetadata(initialDefinition, []);

        cut.Render(parameters => parameters.Add(x => x.WorkflowDefinition, replacementDefinition));

        var fields = cut.FindComponents<MudTextField<string>>();
        Assert.Equal("replacement name", fields[0].Find("input").GetAttribute("value"));
        Assert.Equal("replacement description", fields[1].Find("textarea").GetAttribute("value"));
    }

    [Fact]
    public async Task ExplicitSameVersionReloadAppliesCodeViewMetadataAfterAnEarlierCommit()
    {
        var initialDefinition = CreateDefinition();
        var codeViewDefinition = new WorkflowDefinition
        {
            Id = initialDefinition.Id,
            DefinitionId = initialDefinition.DefinitionId,
            Name = "code view name",
            Description = "code view description"
        };
        var cut = Render<MetadataHost>(parameters => parameters
            .Add(x => x.WorkflowDefinition, initialDefinition)
            .Add(x => x.ReloadVersion, 0));
        var metadata = cut.FindComponent<MetadataComponent>();
        var nameInput = metadata.FindComponents<MudTextField<string>>()[0].Find("input");

        await nameInput.InputAsync("submitted name");
        var validation = _workflowDefinitionService.EnqueueValidation();
        var blurTask = nameInput.BlurAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        validation.Result.SetResult(true);
        await blurTask;

        // The workspace reload signal can be observed before the replacement object reaches this child.
        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, initialDefinition)
            .Add(x => x.ReloadVersion, 1));
        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, codeViewDefinition)
            .Add(x => x.ReloadVersion, 1));

        var fields = cut.FindComponent<MetadataComponent>().FindComponents<MudTextField<string>>();
        Assert.Equal("code view name", fields[0].Find("input").GetAttribute("value"));
        Assert.Equal("code view description", fields[1].Find("textarea").GetAttribute("value"));
    }

    [Fact]
    public async Task ImportCallbackSignalsMetadataReloadBeforeItsDuplicateNotification()
    {
        var initialDefinition = CreateDefinition();
        var importedDefinition = new WorkflowDefinition
        {
            Id = initialDefinition.Id,
            DefinitionId = initialDefinition.DefinitionId,
            Name = "imported name",
            Description = "imported description"
        };
        var reloadVersion = 0L;
        var reloadCount = 0;
        var cut = Render<MetadataHost>(parameters => parameters
            .Add(x => x.WorkflowDefinition, initialDefinition)
            .Add(x => x.ReloadVersion, reloadVersion));
        var nameInput = cut.FindComponent<MetadataComponent>().FindComponents<MudTextField<string>>()[0].Find("input");

        await nameInput.InputAsync("submitted name");
        var validation = _workflowDefinitionService.EnqueueValidation();
        var blurTask = nameInput.BlurAsync();
        await validation.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        validation.Result.SetResult(true);
        await blurTask;

        using var editor = new WorkflowEditor();
        typeof(WorkflowEditor).GetProperty("Mediator", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(editor, new NoOpMediator());

#pragma warning disable BL0005 // The callback is configured directly to exercise the replacement boundary.
        editor.WorkflowDefinitionReloaded = () =>
        {
            reloadCount++;
            reloadVersion++;
            cut.Render(parameters => parameters
                .Add(x => x.WorkflowDefinition, importedDefinition)
                .Add(x => x.ReloadVersion, reloadVersion));
            return Task.CompletedTask;
        };
#pragma warning restore BL0005

        // The importer invokes this callback before publishing ImportedWorkflowDefinition.
        await editor.SetImportedWorkflowDefinitionAsync(importedDefinition);

        var fields = cut.FindComponent<MetadataComponent>().FindComponents<MudTextField<string>>();
        Assert.Equal("imported name", fields[0].Find("input").GetAttribute("value"));
        Assert.Equal("imported description", fields[1].Find("textarea").GetAttribute("value"));

        // The notification is the same import object and must not reset the form a second time.
        await ((INotificationHandler<ImportedWorkflowDefinition>)editor)
            .HandleAsync(new ImportedWorkflowDefinition(importedDefinition), CancellationToken.None);

        Assert.Equal(1, reloadCount);
        Assert.Equal("imported name", importedDefinition.Name);
        Assert.Equal("imported description", importedDefinition.Description);
        Assert.Equal("imported name", fields[0].Find("input").GetAttribute("value"));
        Assert.Equal("imported description", fields[1].Find("textarea").GetAttribute("value"));
    }

    [Fact]
    public async Task ObsoleteQueuedSavePayloadDoesNotTouchTheRendererOrCallbacks()
    {
        using var editor = new WorkflowEditor();
        var editorService = new RecordingWorkflowDefinitionEditorService();
        typeof(WorkflowEditor).GetProperty("Mediator", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(editor, new NoOpMediator());
        typeof(WorkflowEditor).GetProperty("WorkflowDefinitionEditorService", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(editor, editorService);

        editor.InvalidateWorkflowDefinitionOperations();

        var successInvoked = false;
        var failureInvoked = false;
        var saveMethod = typeof(WorkflowEditor).GetMethod("SaveChangesAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var saveTask = (Task)saveMethod.Invoke(editor,
            new object?[]
            {
                false,
                false,
                false,
                (Func<SaveWorkflowDefinitionResponse, Task>)(_ =>
                {
                    successInvoked = true;
                    return Task.CompletedTask;
                }),
                (Func<ValidationErrors, Task>)(_ =>
                {
                    failureInvoked = true;
                    return Task.CompletedTask;
                }),
                0L
            })!;

        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(successInvoked);
        Assert.False(failureInvoked);
        Assert.Equal(0, editorService.SaveCallCount);
    }

    private IRenderedComponent<MetadataComponent> RenderMetadata(WorkflowDefinition definition, List<(string? Name, string? Description)> callbackValues, Func<WorkflowDefinition>? currentDefinition = null) =>
        Render<MetadataComponent>(parameters => parameters
            .Add(x => x.WorkflowDefinition, definition)
            .Add(x => x.WorkflowDefinitionUpdated, () =>
            {
                var callbackDefinition = currentDefinition?.Invoke() ?? definition;
                callbackValues.Add((callbackDefinition.Name, callbackDefinition.Description));
                return Task.CompletedTask;
            }));

    private static WorkflowDefinition CreateDefinition() => new()
    {
        Id = "version-1",
        DefinitionId = "definition-1",
        Name = "initial name",
        Description = "initial description"
    };

    private sealed class RecordingWorkflowDefinitionEditorService : IWorkflowDefinitionEditorService
    {
        public int SaveCallCount { get; private set; }

        public Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(WorkflowDefinition workflowDefinition, bool publish, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            throw new InvalidOperationException("The obsolete save payload reached the editor service.");
        }

        public Task<SaveWorkflowDefinitionResponse> PublishAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowPublishedCallback = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowRetractedCallback = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> ExportAsync(WorkflowDefinition workflowDefinition, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class NoOpMediator : IMediator
    {
        public void Subscribe<TNotification, THandler>(THandler handler)
            where TNotification : INotification
            where THandler : INotificationHandler<TNotification>
        {
        }

        public void Unsubscribe<TNotification, THandler>(THandler handler)
            where TNotification : INotification
            where THandler : INotificationHandler<TNotification>
        {
        }

        public void Unsubscribe(INotificationHandler handler)
        {
        }

        public Task NotifyAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    private sealed class MetadataHost : ComponentBase
    {
        [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
        [Parameter] public long ReloadVersion { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<long>>(0);
            builder.AddAttribute(1, nameof(CascadingValue<long>.Name), "WorkflowDefinitionReloadVersion");
            builder.AddAttribute(2, nameof(CascadingValue<long>.Value), ReloadVersion);
            builder.AddAttribute(3, nameof(CascadingValue<long>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MetadataComponent>(0);
                childBuilder.AddAttribute(1, nameof(MetadataComponent.WorkflowDefinition), WorkflowDefinition);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}

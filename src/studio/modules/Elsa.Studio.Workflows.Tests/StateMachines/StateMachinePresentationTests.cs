using System.Text.Json.Nodes;
using Bunit;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;
using Elsa.Studio.Workflows.Tests.Support;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Workflows.Tests.StateMachines;

public sealed class StateMachinePresentationTests : BunitContext, IAsyncLifetime
{
    public StateMachinePresentationTests()
    {
        Services.AddSingleton<ILocalizer, TestLocalizer>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void Outline_ExposesSelectionAndStateStatusWithoutRelyingOnColor()
    {
        var pending = new StateMachineStateNode { Name = "Pending" };
        var approved = new StateMachineStateNode { Name = "Approved", IsTerminal = true };
        var graph = new StateMachineGraph
        {
            InitialState = pending.Name,
            CurrentState = pending.Name,
            States = { pending, approved },
            Transitions =
            {
                new StateMachineTransitionEdge
                {
                    Name = "Approve",
                    From = pending.Name,
                    To = approved.Name,
                    Trigger = new JsonObject { ["type"] = "Button" }
                }
            }
        };

        var cut = Render<StateMachineOutline>(parameters => parameters
            .Add(component => component.Graph, graph)
            .Add(component => component.SelectedStateName, pending.Name));

        var selectedState = cut.Find("[data-state-name='Pending']");
        Assert.Equal("true", selectedState.GetAttribute("aria-pressed"));
        Assert.Contains("Initial", selectedState.TextContent);
        Assert.Contains("Current", selectedState.TextContent);
        Assert.Contains("1 outgoing transition", selectedState.TextContent);

        var terminalState = cut.Find("[data-state-name='Approved']");
        Assert.Equal("false", terminalState.GetAttribute("aria-pressed"));
        Assert.Contains("Terminal", terminalState.TextContent);

        var transition = cut.Find("[data-transition-index='0']");
        Assert.Contains("Pending", transition.TextContent);
        Assert.Contains("Approved", transition.TextContent);
        Assert.Contains("Trigger", transition.TextContent);
    }

    [Fact]
    public void Outline_EmitsTheSelectedGraphItem()
    {
        var state = new StateMachineStateNode { Name = "Pending" };
        var transition = new StateMachineTransitionEdge { From = "Pending", To = "Approved" };
        var graph = new StateMachineGraph { States = { state }, Transitions = { transition } };
        StateMachineStateNode? selectedState = null;
        StateMachineTransitionEdge? selectedTransition = null;
        var cut = Render<StateMachineOutline>(parameters => parameters
            .Add(component => component.Graph, graph)
            .Add(component => component.StateSelected, value => selectedState = value)
            .Add(component => component.TransitionSelected, value => selectedTransition = value));

        cut.Find("[data-state-name='Pending']").Click();
        cut.Find("[data-transition-index='0']").Click();

        Assert.Same(state, selectedState);
        Assert.Same(transition, selectedTransition);
    }

    [Fact]
    public void StateInspector_RendersLifecycleAndEmitsSemanticActionsWithoutMutatingTheModel()
    {
        var state = new StateMachineStateNode
        {
            Name = "Pending",
            Entry = Activity("Elsa.WriteLine", "Entry1", "Workflow1:Entry1")
        };
        string? changedName = null;
        var actions = new List<string>();
        var viewedTransitions = false;
        var cut = Render<StateMachineStateInspector>(parameters => parameters
            .Add(component => component.State, state)
            .Add(component => component.IsInitial, true)
            .Add(component => component.IsCurrent, true)
            .Add(component => component.SelectedActivityId, "Entry1")
            .Add(component => component.IncomingTransitionCount, 1)
            .Add(component => component.OutgoingTransitionCount, 2)
            .Add(component => component.NameChanged, value => changedName = value)
            .Add(component => component.ActivitySelectRequested, slot => actions.Add($"select:{slot}"))
            .Add(component => component.ActivityJsonRequested, slot => actions.Add($"json:{slot}"))
            .Add(component => component.ActivityReplaceRequested, slot => actions.Add($"replace:{slot}"))
            .Add(component => component.ActivityClearRequested, slot => actions.Add($"clear:{slot}"))
            .Add(component => component.ActivityAddRequested, slot => actions.Add($"add:{slot}"))
            .Add(component => component.ActivityDropRequested, slot => actions.Add($"drop:{slot}"))
            .Add(component => component.ViewTransitionsRequested, () => viewedTransitions = true));

        var nameInput = cut.Find("input[id$='-name']");
        var nameLabel = cut.Find($"label[for='{nameInput.Id}']");
        Assert.Equal("Name", nameLabel.TextContent);
        var markup = cut.Markup;
        Assert.True(markup.IndexOf("ON ENTRY", StringComparison.Ordinal) < markup.IndexOf("ACTIVE", StringComparison.Ordinal));
        Assert.True(markup.IndexOf("ACTIVE", StringComparison.Ordinal) < markup.IndexOf("ON EXIT", StringComparison.Ordinal));
        Assert.Contains("Initial", cut.Find("[aria-label='State status']").TextContent);
        Assert.Contains("Current", cut.Find("[aria-label='State status']").TextContent);
        Assert.Contains("2 outgoing transitions", cut.Find("[data-state-stage='active']").TextContent);
        Assert.Contains("Elsa.WriteLine", cut.Find("[data-state-stage='entry']").TextContent);
        Assert.Equal("true", cut.Find("[data-state-stage='entry'] [data-activity-selected]").GetAttribute("data-activity-selected"));
        Assert.Contains("No exit activity configured", cut.Find("[data-state-stage='exit']").TextContent);

        nameInput.Change("Review");
        cut.Find("[data-state-stage='entry'] [data-slot-action='select']").Click();
        cut.Find("[data-state-stage='entry'] [data-slot-action='json']").Click();
        cut.Find("[data-state-stage='entry'] [data-slot-action='replace']").Click();
        cut.Find("[data-state-stage='entry'] [data-slot-action='clear']").Click();
        cut.Find("[data-state-stage='entry'] [data-slot-action='drop']").TriggerEvent("ondrop", new DragEventArgs());
        cut.Find("[data-state-stage='exit'] [data-slot-action='add']").Click();
        cut.Find("[data-testid='state-machine-state-view-transitions']").Click();

        Assert.Equal("Review", changedName);
        Assert.Equal("Pending", state.Name);
        Assert.Equal(["select:entry", "json:entry", "replace:entry", "clear:entry", "drop:entry", "add:exit"], actions);
        Assert.True(viewedTransitions);
        Assert.DoesNotContain("textarea", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void StateInspector_ReadOnlyModeSuppressesDestructiveActionsAndDisablesFields()
    {
        var cut = Render<StateMachineStateInspector>(parameters => parameters
            .Add(component => component.State, new StateMachineStateNode
            {
                Name = "Pending",
                Entry = Activity("Elsa.WriteLine", "Entry1", "Workflow1:Entry1")
            })
            .Add(component => component.IsReadOnly, true));

        Assert.True(cut.Find("input[id$='-name']").HasAttribute("disabled"));
        var propertiesButton = Assert.Single(cut.FindAll("[data-slot-action='select']"));
        Assert.Equal("View properties", propertiesButton.TextContent.Trim());
        Assert.Equal("View entry activity properties", propertiesButton.GetAttribute("aria-label"));
        Assert.Single(cut.FindAll("[data-slot-action='json']"));
        Assert.Empty(cut.FindAll("[data-slot-action='open']"));
        Assert.Empty(cut.FindAll("[data-slot-action='add']"));
        Assert.Empty(cut.FindAll("[data-slot-action='replace']"));
        Assert.Empty(cut.FindAll("[data-slot-action='clear']"));
        Assert.Empty(cut.FindAll("[data-testid='state-machine-state-delete']"));
        Assert.All(cut.FindAll("[data-slot-action='drop']"), element => Assert.False(element.HasAttribute("ondrop")));
    }

    [Fact]
    public void TransitionInspector_RendersExecutionStoryAndSemanticSlotSummaries()
    {
        var transition = new StateMachineTransitionEdge
        {
            Name = "Approve",
            From = "Pending",
            To = "Approved",
            Trigger = Activity("Elsa.Event", "Trigger1", "Workflow1:Trigger1"),
            Condition = JsonValue.Create(true),
            Action = Activity("Elsa.WriteLine", "Action1", "Workflow1:Action1", ("text", "Approved"))
        };
        var states = new[]
        {
            new StateMachineStateNode { Name = "Pending" },
            new StateMachineStateNode { Name = "Approved" }
        };
        string? editedCondition = null;
        string? changedFrom = null;
        string? changedName = "not-called";
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters
            .Add(component => component.Transition, transition)
            .Add(component => component.States, states)
            .Add(component => component.ConditionEditRequested, value => editedCondition = value)
            .Add(component => component.FromChanged, value => changedFrom = value)
            .Add(component => component.NameChanged, value => changedName = value));

        var markup = cut.Markup;
        Assert.True(markup.IndexOf("WHEN", StringComparison.Ordinal) < markup.IndexOf("ONLY IF", StringComparison.Ordinal));
        Assert.True(markup.IndexOf("ONLY IF", StringComparison.Ordinal) < markup.IndexOf("THEN", StringComparison.Ordinal));
        Assert.True(markup.IndexOf("THEN", StringComparison.Ordinal) < markup.IndexOf("TO", StringComparison.Ordinal));
        Assert.Equal(4, cut.FindAll("[data-transition-slot]").Count);
        Assert.Contains("Elsa.Event", cut.Find("[data-transition-slot='trigger']").TextContent);
        Assert.Contains("Elsa.WriteLine", cut.Find("[data-transition-slot='action']").TextContent);
        Assert.Equal("always", cut.Find("[data-transition-slot='condition'] [data-condition-state]").GetAttribute("data-condition-state"));
        Assert.NotEmpty(cut.FindAll("[data-slot-action='select']"));
        Assert.NotEmpty(cut.FindAll("[data-slot-action='json']"));
        Assert.Empty(cut.FindAll("[data-slot-action='open']"));
        Assert.NotEmpty(cut.FindAll("[data-slot-action='replace']"));
        Assert.NotEmpty(cut.FindAll("[data-slot-action='clear']"));

        cut.Find("select[id$='-from']").Change("Approved");
        cut.Find("input[id$='-name']").Change("");
        cut.Find("[data-transition-slot='condition'] [data-slot-action='edit']").Click();

        Assert.Equal("Approved", changedFrom);
        Assert.Null(changedName);
        Assert.Equal("condition", editedCondition);
        Assert.True(transition.Condition!.GetValue<bool>());
        Assert.Equal("Pending", transition.From);
        Assert.DoesNotContain("textarea", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TransitionInspector_DescribesMissingAndFalseConditionsWithoutMutatingThem()
    {
        var missing = new StateMachineTransitionEdge { From = "Pending", To = "Approved" };
        var missingCut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition, missing));

        Assert.Contains("No trigger configured", missingCut.Find("[data-transition-slot='trigger']").TextContent);
        Assert.Contains("evaluates immediately", missingCut.Find("[data-transition-slot='trigger']").TextContent);
        Assert.Equal("missing", missingCut.Find("[data-transition-slot='condition'] [data-condition-state]").GetAttribute("data-condition-state"));
        Assert.Contains("Always", missingCut.Find("[data-transition-slot='condition']").TextContent);
        Assert.Null(missing.Condition);

        var falseCondition = new StateMachineTransitionEdge
        {
            From = "Pending",
            To = "Approved",
            Condition = JsonValue.Create(false)
        };
        var falseCut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition, falseCondition));

        var condition = falseCut.Find("[data-transition-slot='condition']");
        Assert.Equal("never", condition.QuerySelector("[data-condition-state]")!.GetAttribute("data-condition-state"));
        Assert.Contains("Never", condition.TextContent);
        Assert.Contains("cannot pass", condition.TextContent);
        Assert.False(falseCondition.Condition!.GetValue<bool>());
    }

    [Fact]
    public void TransitionInspector_DescribesCanonicalWrappedBooleanConditionsWithoutNormalizingThem()
    {
        var trueCondition = new JsonObject
        {
            ["typeName"] = "Boolean",
            ["expression"] = new JsonObject { ["type"] = "Literal", ["value"] = true }
        };
        var falseCondition = new JsonObject
        {
            ["typeName"] = "Boolean",
            ["expression"] = new JsonObject { ["type"] = "Literal", ["value"] = false }
        };
        var trueSource = trueCondition.ToJsonString();
        var falseSource = falseCondition.ToJsonString();

        var trueCut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition,
            new StateMachineTransitionEdge { From = "Pending", To = "Approved", Condition = trueCondition }));
        var falseCut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition,
            new StateMachineTransitionEdge { From = "Pending", To = "Approved", Condition = falseCondition }));

        Assert.Equal("always", trueCut.Find("[data-condition-state]").GetAttribute("data-condition-state"));
        Assert.Contains("Always", trueCut.Find("[data-testid='state-machine-transition-condition-summary']").TextContent);
        Assert.Equal("never", falseCut.Find("[data-condition-state]").GetAttribute("data-condition-state"));
        Assert.Contains("Never", falseCut.Find("[data-testid='state-machine-transition-condition-summary']").TextContent);
        Assert.Equal(trueSource, trueCondition.ToJsonString());
        Assert.Equal(falseSource, falseCondition.ToJsonString());
    }

    [Fact]
    public void TransitionInspector_DescribesCanonicalWrappedProviderExpression()
    {
        var condition = new JsonObject
        {
            ["typeName"] = "Boolean",
            ["expression"] = new JsonObject { ["type"] = "JavaScript", ["value"] = "context.input === true" }
        };
        var source = condition.ToJsonString();
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition,
            new StateMachineTransitionEdge { From = "Pending", To = "Approved", Condition = condition }));

        var summary = cut.Find("[data-testid='state-machine-transition-condition-summary']");
        Assert.Equal("expression", summary.GetAttribute("data-condition-state"));
        Assert.Contains("JavaScript", summary.TextContent);
        Assert.Contains("context.input === true", summary.TextContent);
        Assert.Equal(source, condition.ToJsonString());
    }

    [Fact]
    public void TransitionInspector_RendersExpressionLanguageAndValueAsDistinctElements()
    {
        var condition = new JsonObject
        {
            ["typeName"] = "Boolean",
            ["expression"] = new JsonObject { ["type"] = "JavaScript", ["value"] = "return 1 == 1" }
        };
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition,
            new StateMachineTransitionEdge { From = "Pending", To = "Approved", Condition = condition }));

        var summary = cut.Find("[data-testid='state-machine-transition-condition-summary']");
        var language = summary.QuerySelector("[data-condition-language]");
        var expression = summary.QuerySelector("[data-condition-expression]");

        Assert.NotNull(language);
        Assert.Equal("JavaScript", language!.TextContent);
        Assert.NotNull(expression);
        Assert.Equal("return 1 == 1", expression!.TextContent);
        Assert.NotSame(language, expression);
    }

    [Fact]
    public void TransitionInspector_PreservesCanonicalWrappedUnavailableProvider()
    {
        var condition = new JsonObject
        {
            ["typeName"] = "Boolean",
            ["expression"] = new JsonObject { ["type"] = "Liquid", ["value"] = "order.total > 0" }
        };
        var source = condition.ToJsonString();
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition,
            new StateMachineTransitionEdge { From = "Pending", To = "Approved", Condition = condition }));

        var summary = cut.Find("[data-testid='state-machine-transition-condition-summary']");
        Assert.Equal("unknown", summary.GetAttribute("data-condition-state"));
        Assert.Contains("Liquid", summary.TextContent);
        Assert.Contains("\"typeName\": \"Boolean\"", cut.Find("[data-testid='state-machine-transition-condition-definition']").TextContent);
        Assert.Equal(source, condition.ToJsonString());
    }

    [Fact]
    public void TransitionInspector_RecognizesProviderAdvertisedByTheBackend()
    {
        var condition = new JsonObject
        {
            ["typeName"] = "Boolean",
            ["expression"] = new JsonObject { ["type"] = "Liquid", ["value"] = "order.total > 0" }
        };
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters
            .Add(component => component.Transition,
                new StateMachineTransitionEdge { From = "Pending", To = "Approved", Condition = condition })
            .Add(component => component.KnownExpressionProviderTypes, ["JavaScript", "Liquid"]));

        var summary = cut.Find("[data-testid='state-machine-transition-condition-summary']");
        Assert.Equal("expression", summary.GetAttribute("data-condition-state"));
        Assert.Contains("Liquid", summary.TextContent);
    }

    [Fact]
    public void TransitionInspector_ClassifiesPartialActivityAsMalformedAndDoesNotOfferOpen()
    {
        var action = new JsonObject { ["type"] = "Elsa.WriteLine", ["text"] = "incomplete" };
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition,
            new StateMachineTransitionEdge { From = "Pending", To = "Approved", Action = action }));

        var slot = "[data-transition-slot='action']";
        Assert.Equal("malformed", cut.Find($"{slot} [data-activity-state]").GetAttribute("data-activity-state"));
        Assert.Contains("incomplete", cut.Find($"{slot} [data-testid='state-machine-transition-action-definition']").TextContent);
        Assert.Empty(cut.FindAll($"{slot} [data-slot-action='open']"));
        Assert.NotEmpty(cut.FindAll($"{slot} [data-slot-action='replace']"));
        Assert.NotEmpty(cut.FindAll($"{slot} [data-slot-action='clear']"));
        Assert.Equal("Elsa.WriteLine", action["type"]!.GetValue<string>());
    }

    [Fact]
    public void TransitionInspector_PreservesMalformedAndUnknownDefinitionsForInspection()
    {
        var transition = new StateMachineTransitionEdge
        {
            From = "Pending",
            To = "Approved",
            Trigger = new JsonObject { ["id"] = "LegacyTrigger", ["payload"] = "keep-me" },
            Condition = new JsonObject { ["type"] = "Contoso.CustomCondition", ["payload"] = "keep-me" },
            Action = JsonValue.Create("not-an-activity")
        };

        var cut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition, transition));

        Assert.Equal("malformed", cut.Find("[data-transition-slot='trigger'] [data-activity-state]").GetAttribute("data-activity-state"));
        Assert.Contains("keep-me", cut.Find("[data-testid='state-machine-transition-trigger-definition']").TextContent);
        Assert.Equal("unknown", cut.Find("[data-transition-slot='condition'] [data-condition-state]").GetAttribute("data-condition-state"));
        Assert.Contains("Contoso.CustomCondition", cut.Find("[data-testid='state-machine-transition-condition-definition']").TextContent);
        Assert.Equal("malformed", cut.Find("[data-transition-slot='action'] [data-activity-state]").GetAttribute("data-activity-state"));
        Assert.Contains("not-an-activity", cut.Find("[data-testid='state-machine-transition-action-definition']").TextContent);
        Assert.NotEmpty(cut.FindAll("[data-transition-slot='trigger'] [data-slot-action='replace']"));
        Assert.Empty(cut.FindAll("[data-transition-slot='trigger'] [data-slot-action='open']"));
    }

    [Fact]
    public void TransitionInspector_PreservesUnknownAndMalformedJsonDuringInspection()
    {
        var trigger = new JsonObject { ["type"] = "Contoso.Trigger", ["opaque"] = new JsonObject { ["value"] = 42 } };
        var condition = new JsonObject
        {
            [StateMachineDesignerConstants.InvalidJsonSlotProperty] = StateMachineDesignerConstants.InvalidJsonSlotMarkerValue,
            [StateMachineDesignerConstants.InvalidJsonSlotSourceProperty] = "{ broken"
        };
        var action = JsonValue.Create("legacy-action");
        var triggerSource = trigger.DeepClone();
        var conditionSource = condition.DeepClone();
        var actionSource = action.DeepClone();
        var transition = new StateMachineTransitionEdge
        {
            From = "Pending",
            To = "Approved",
            Trigger = trigger,
            Condition = condition,
            Action = action
        };

        Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition, transition));

        Assert.True(JsonNode.DeepEquals(triggerSource, transition.Trigger));
        Assert.True(JsonNode.DeepEquals(conditionSource, transition.Condition));
        Assert.True(JsonNode.DeepEquals(actionSource, transition.Action));
    }

    [Fact]
    public void TransitionInspector_EmitsSemanticActivityCallbacksWithSlotNames()
    {
        var slots = new List<string>();
        var configured = new StateMachineTransitionEdge
        {
            From = "Pending",
            To = "Approved",
            Trigger = Activity("Elsa.Event", "Trigger1", "Workflow1:Trigger1"),
            Action = Activity("Elsa.WriteLine", "Action1", "Workflow1:Action1")
        };
        var configuredCut = Render<StateMachineTransitionInspector>(parameters => parameters
            .Add(component => component.Transition, configured)
            .Add(component => component.TriggerSupportsDesigner, true)
            .Add(component => component.ActivityOpenRequested, slot => slots.Add($"open:{slot}"))
            .Add(component => component.ActivityReplaceRequested, slot => slots.Add($"replace:{slot}"))
            .Add(component => component.ActivityClearRequested, slot => slots.Add($"clear:{slot}"))
            .Add(component => component.ActivityDropRequested, slot => slots.Add($"drop:{slot}")));

        configuredCut.Find("[data-transition-slot='trigger'] [data-slot-action='open']").Click();
        configuredCut.Find("[data-transition-slot='action'] [data-slot-action='replace']").Click();
        configuredCut.Find("[data-transition-slot='action'] [data-slot-action='clear']").Click();
        configuredCut.Find("[data-transition-slot='trigger'] [data-slot-action='drop']").TriggerEvent("ondrop", new DragEventArgs());

        var emptyCut = Render<StateMachineTransitionInspector>(parameters => parameters
            .Add(component => component.Transition, new StateMachineTransitionEdge { From = "Pending", To = "Approved" })
            .Add(component => component.ActivityAddRequested, slot => slots.Add($"add:{slot}")));
        emptyCut.Find("[data-transition-slot='action'] [data-slot-action='add']").Click();

        Assert.Equal(["open:trigger", "replace:action", "clear:action", "drop:trigger", "add:action"], slots);
    }

    [Fact]
    public void TransitionInspector_ReadOnlyModeRetainsInspectionButSuppressesMutationControlsAndDrops()
    {
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters
            .Add(component => component.Transition, new StateMachineTransitionEdge
            {
                From = "Pending",
                To = "Approved",
                Trigger = Activity("Elsa.Event", "Trigger1", "Workflow1:Trigger1"),
                Condition = JsonValue.Create(false),
                Action = Activity("Elsa.WriteLine", "Action1", "Workflow1:Action1")
            })
            .Add(component => component.IsReadOnly, true));

        Assert.Contains("WHEN", cut.Markup);
        Assert.Contains("Never", cut.Markup);
        Assert.Empty(cut.FindAll("[data-slot-action='open']"));
        var propertiesButtons = cut.FindAll("[data-slot-action='select']");
        Assert.Equal(2, propertiesButtons.Count);
        Assert.All(propertiesButtons, button => Assert.Equal("View properties", button.TextContent.Trim()));
        Assert.Equal("View trigger activity properties", propertiesButtons[0].GetAttribute("aria-label"));
        Assert.Equal("View action activity properties", propertiesButtons[1].GetAttribute("aria-label"));
        Assert.Equal(2, cut.FindAll("[data-slot-action='json']").Count);
        Assert.Empty(cut.FindAll("[data-slot-action='add']"));
        Assert.Empty(cut.FindAll("[data-slot-action='replace']"));
        Assert.Empty(cut.FindAll("[data-slot-action='clear']"));
        Assert.Empty(cut.FindAll("[data-slot-action='edit']"));
        Assert.All(cut.FindAll("[data-slot-action='drop']"), element => Assert.False(element.HasAttribute("ondrop")));
    }

    [Fact]
    public void TransitionInspector_ReadOnlyModeAllowsOpenButNeverInvokesDropOrMutatesSlots()
    {
        var transition = new StateMachineTransitionEdge
        {
            From = "Pending",
            To = "Approved",
            Trigger = Activity("Elsa.Event", "Trigger1", "Workflow1:Trigger1", ("payload", "keep-trigger")),
            Action = Activity("Elsa.WriteLine", "Action1", "Workflow1:Action1", ("payload", "keep-action"))
        };
        var triggerSource = transition.Trigger.DeepClone();
        var actionSource = transition.Action.DeepClone();
        var opened = new List<string>();
        var dropped = new List<string>();
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters
            .Add(component => component.Transition, transition)
            .Add(component => component.IsReadOnly, true)
            .Add(component => component.TriggerSupportsDesigner, true)
            .Add(component => component.ActionSupportsDesigner, true)
            .Add(component => component.ActivityOpenRequested, slot => opened.Add(slot))
            .Add(component => component.ActivityDropRequested, slot => dropped.Add(slot)));

        cut.Find("[data-transition-slot='trigger'] [data-slot-action='open']").Click();
        cut.Find("[data-transition-slot='action'] [data-slot-action='open']").Click();

        Assert.Equal(["trigger", "action"], opened);
        Assert.Empty(dropped);
        Assert.All(cut.FindAll("[data-slot-action='drop']"), element => Assert.False(element.HasAttribute("ondrop")));
        Assert.True(JsonNode.DeepEquals(triggerSource, transition.Trigger));
        Assert.True(JsonNode.DeepEquals(actionSource, transition.Action));
    }

    [Fact]
    public void TransitionInspector_UsesKeyboardButtonsWithStableAccessibleNamesForEveryMutationAction()
    {
        var transition = new StateMachineTransitionEdge
        {
            From = "Pending",
            To = "Approved",
            Trigger = Activity("Elsa.Event", "Trigger1", "Workflow1:Trigger1"),
            Condition = JsonValue.Create(true),
            Action = Activity("Elsa.WriteLine", "Action1", "Workflow1:Action1")
        };
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters
            .Add(component => component.Transition, transition)
            .Add(component => component.TriggerSupportsDesigner, true)
            .Add(component => component.ActionSupportsDesigner, true));

        var actionButtons = cut.FindAll("[data-transition-slot] button");
        Assert.NotEmpty(actionButtons);
        Assert.All(actionButtons, button =>
        {
            Assert.Equal("button", button.GetAttribute("type"));
            Assert.False(string.IsNullOrWhiteSpace(button.GetAttribute("aria-label")) && string.IsNullOrWhiteSpace(button.TextContent));
        });

        foreach (var action in new[] { "select", "open", "json", "replace", "clear" })
        {
            var triggerButton = cut.Find($"[data-transition-slot='trigger'] [data-slot-action='{action}']");
            var activityButton = cut.Find($"[data-transition-slot='action'] [data-slot-action='{action}']");
            Assert.False(string.IsNullOrWhiteSpace(triggerButton.GetAttribute("aria-label")));
            Assert.False(string.IsNullOrWhiteSpace(activityButton.GetAttribute("aria-label")));
        }

        var emptyCut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition,
            new StateMachineTransitionEdge { From = "Pending", To = "Approved" }));
        foreach (var slot in new[] { "trigger", "action" })
        {
            var addButton = emptyCut.Find($"[data-transition-slot='{slot}'] [data-slot-action='add']");
            Assert.Equal("button", addButton.GetAttribute("type"));
            Assert.False(string.IsNullOrWhiteSpace(addButton.GetAttribute("aria-label")));
        }

        var conditionEdit = cut.Find("[data-transition-slot='condition'] [data-slot-action='edit']");
        Assert.Equal("state-machine-transition-condition-edit", conditionEdit.GetAttribute("data-testid"));
        Assert.Contains("Edit", conditionEdit.TextContent);
    }

    private static JsonObject Activity(string type, string id, string nodeId, params (string Name, string Value)[] properties)
    {
        var activity = new JsonObject
        {
            ["type"] = type,
            ["id"] = id,
            ["nodeId"] = nodeId
        };

        foreach (var (name, value) in properties)
            activity[name] = value;

        return activity;
    }
}

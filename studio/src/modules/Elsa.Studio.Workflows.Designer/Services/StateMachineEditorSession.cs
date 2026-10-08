using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using static Elsa.Studio.Workflows.Designer.StateMachineDesignerConstants;

namespace Elsa.Studio.Workflows.Designer.Services;

/// <summary>
/// Owns StateMachine editing commands and exposes a renderer-neutral canvas projection.
/// Runtime JSON remains authoritative; visual IDs and positions live only for this session.
/// </summary>
public sealed class StateMachineEditorSession
{
    private const double DefaultHorizontalGap = 280;
    private readonly IStateMachineMapper _mapper;
    private readonly StateMachineValidator _validator;
    private readonly List<StateMachineValidationIssue> _structuralIssues;
    private readonly Dictionary<StateMachineStateNode, string> _stateVisualIds = [];
    private readonly Dictionary<string, StateMachineStateNode> _statesByVisualId = new(StringComparer.Ordinal);
    private readonly Dictionary<StateMachineTransitionEdge, string> _transitionVisualIds = [];
    private readonly Dictionary<string, StateMachineTransitionEdge> _transitionsByVisualId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StateMachineCanvasPosition> _statePositions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<StateMachineCanvasPosition>> _transitionVertices = new(StringComparer.Ordinal);
    private int _nextStateVisualId;
    private int _nextTransitionVisualId;
    private int _nextDefaultStatePositionIndex;

    public StateMachineEditorSession(
        IStateMachineMapper mapper,
        StateMachineValidator validator,
        JsonObject activity)
    {
        _mapper = mapper;
        _validator = validator;
        Graph = mapper.Map(activity);
        _structuralIssues = Graph.ValidationIssues.Where(IsStructuralIssue).ToList();

        foreach (var state in Graph.States)
        {
            var visualId = RegisterState(state);
            _statePositions[visualId] = CreateNextDefaultStatePosition();
        }

        foreach (var transition in Graph.Transitions)
            RegisterTransition(transition);

        RefreshDerivedState();
    }

    /// <summary>
    /// Gets the editable semantic graph.
    /// </summary>
    public StateMachineGraph Graph { get; }

    /// <summary>
    /// Gets current structural and semantic validation issues.
    /// </summary>
    public IReadOnlyCollection<StateMachineValidationIssue> ValidationIssues => Graph.ValidationIssues.ToList();

    /// <summary>
    /// Gets a value indicating whether the graph can be exported safely.
    /// </summary>
    public bool CanExport => Graph.ValidationIssues.All(x => x.Severity != StateMachineValidationSeverity.Error);

    /// <summary>
    /// Gets a state by its stable visual identity.
    /// </summary>
    public StateMachineStateNode GetState(string visualId) =>
        _statesByVisualId.TryGetValue(visualId, out var state)
            ? state
            : throw new KeyNotFoundException($"State visual ID '{visualId}' was not found.");

    /// <summary>
    /// Gets a transition by its stable visual identity.
    /// </summary>
    public StateMachineTransitionEdge GetTransition(string visualId) =>
        _transitionsByVisualId.TryGetValue(visualId, out var transition)
            ? transition
            : throw new KeyNotFoundException($"Transition visual ID '{visualId}' was not found.");

    /// <summary>
    /// Gets the stable visual identity for a state model.
    /// </summary>
    public string GetStateVisualId(StateMachineStateNode state) => _stateVisualIds[state];

    /// <summary>
    /// Gets the stable visual identity for a transition model.
    /// </summary>
    public string GetTransitionVisualId(StateMachineTransitionEdge transition) => _transitionVisualIds[transition];

    /// <summary>
    /// Projects the current semantic graph without changing its order or runtime JSON.
    /// </summary>
    public StateMachineCanvasGraph ProjectCanvas()
    {
        var uniqueStatesByName = Graph.States
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name, StringComparer.Ordinal)
            .Where(x => x.Count() == 1)
            .ToDictionary(x => x.Key, x => _stateVisualIds[x.Single()], StringComparer.Ordinal);

        return new()
        {
            InitialState = Graph.InitialState,
            CurrentState = Graph.CurrentState,
            States = Graph.States.Select((state, index) =>
            {
                var issues = GetStateIssues(state);
                return new StateMachineCanvasState
                {
                    VisualId = _stateVisualIds[state],
                    Name = state.Name,
                    SemanticIndex = index,
                    Position = _statePositions[_stateVisualIds[state]],
                    IsInitial = string.Equals(state.Name, Graph.InitialState, StringComparison.Ordinal),
                    IsCurrent = string.Equals(state.Name, Graph.CurrentState, StringComparison.Ordinal),
                    IsTerminal = state.IsTerminal,
                    HasEntry = state.Entry != null,
                    HasExit = state.Exit != null,
                    ValidationIssueCount = issues.Count,
                    HasValidationErrors = issues.Any(x => x.Severity == StateMachineValidationSeverity.Error)
                };
            }).ToList(),
            Transitions = Graph.Transitions.Select((transition, index) =>
            {
                var issues = GetTransitionIssues(transition);
                return new StateMachineCanvasTransition
                {
                    VisualId = _transitionVisualIds[transition],
                    SemanticIndex = index,
                    Name = transition.Name,
                    DisplayName = transition.DisplayName,
                    From = transition.From,
                    To = transition.To,
                    SourceStateVisualId = uniqueStatesByName.GetValueOrDefault(transition.From),
                    TargetStateVisualId = uniqueStatesByName.GetValueOrDefault(transition.To),
                    HasTrigger = transition.Trigger != null,
                    HasCondition = transition.Condition != null,
                    HasAction = transition.Action != null,
                    ValidationIssueCount = issues.Count,
                    HasValidationErrors = issues.Any(x => x.Severity == StateMachineValidationSeverity.Error),
                    Vertices = _transitionVertices.GetValueOrDefault(_transitionVisualIds[transition], [])
                };
            }).ToList()
        };
    }

    public string AddState(string name)
    {
        EnsureEditableStructure();
        var normalizedName = NormalizeRequiredName(name, nameof(name));
        EnsureUniqueStateName(normalizedName);

        var state = new StateMachineStateNode
        {
            Name = normalizedName,
            Source = new JsonObject { ["name"] = normalizedName }
        };
        Graph.States.Add(state);
        Graph.InitialState ??= normalizedName;
        Graph.CurrentState ??= normalizedName;

        var visualId = RegisterState(state);
        _statePositions[visualId] = CreateNextDefaultStatePosition();
        RefreshDerivedState();
        return visualId;
    }

    public void RenameState(string visualId, string name)
    {
        EnsureEditableStructure();
        var state = GetState(visualId);
        var normalizedName = NormalizeRequiredName(name, nameof(name));
        EnsureUniqueStateName(normalizedName, state);
        var oldName = state.Name;

        if (string.Equals(oldName, normalizedName, StringComparison.Ordinal))
            return;

        state.Name = normalizedName;
        foreach (var transition in Graph.Transitions)
        {
            if (string.Equals(transition.From, oldName, StringComparison.Ordinal))
                transition.From = normalizedName;
            if (string.Equals(transition.To, oldName, StringComparison.Ordinal))
                transition.To = normalizedName;
        }

        if (string.Equals(Graph.InitialState, oldName, StringComparison.Ordinal))
            Graph.InitialState = normalizedName;
        if (string.Equals(Graph.CurrentState, oldName, StringComparison.Ordinal))
            Graph.CurrentState = normalizedName;

        RefreshDerivedState();
    }

    public void DeleteState(string visualId)
    {
        EnsureEditableStructure();
        var state = GetState(visualId);
        var connectedTransitions = Graph.Transitions.Where(x =>
            string.Equals(x.From, state.Name, StringComparison.Ordinal) ||
            string.Equals(x.To, state.Name, StringComparison.Ordinal)).ToList();

        foreach (var transition in connectedTransitions)
            RemoveTransition(transition);

        Graph.States.Remove(state);
        _statesByVisualId.Remove(visualId);
        _stateVisualIds.Remove(state);
        _statePositions.Remove(visualId);

        if (string.Equals(Graph.InitialState, state.Name, StringComparison.Ordinal))
            Graph.InitialState = Graph.States.FirstOrDefault()?.Name;
        if (string.Equals(Graph.CurrentState, state.Name, StringComparison.Ordinal))
            Graph.CurrentState = Graph.States.FirstOrDefault()?.Name;

        RefreshDerivedState();
    }

    public string AddTransition(
        string sourceStateVisualId,
        string targetStateVisualId,
        string? name = null,
        string? displayName = null)
    {
        EnsureEditableStructure();
        var source = GetState(sourceStateVisualId);
        var target = GetState(targetStateVisualId);
        var normalizedName = NormalizeOptional(name);

        if (Graph.Transitions.Any(x =>
                string.Equals(x.Name, normalizedName, StringComparison.Ordinal) &&
                string.Equals(x.From, source.Name, StringComparison.Ordinal) &&
                string.Equals(x.To, target.Name, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("A transition with the same name, source, and target already exists.");
        }

        var transition = new StateMachineTransitionEdge
        {
            Name = normalizedName,
            DisplayName = NormalizeOptional(displayName),
            From = source.Name,
            To = target.Name,
            Source = new JsonObject
            {
                ["from"] = source.Name,
                ["to"] = target.Name
            }
        };
        Graph.Transitions.Add(transition);

        var visualId = RegisterTransition(transition);
        RefreshDerivedState();
        return visualId;
    }

    public void DeleteTransition(string visualId)
    {
        EnsureEditableStructure();
        RemoveTransition(GetTransition(visualId));
        RefreshDerivedState();
    }

    public void SetInitialState(string? stateVisualId)
    {
        EnsureEditableStructure();
        Graph.InitialState = stateVisualId == null ? null : GetState(stateVisualId).Name;
        RefreshDerivedState();
    }

    public void SetCurrentState(string? stateVisualId)
    {
        EnsureEditableStructure();
        Graph.CurrentState = stateVisualId == null ? null : GetState(stateVisualId).Name;
        RefreshDerivedState();
    }

    public void SetTransitionEndpoints(string transitionVisualId, string sourceStateVisualId, string targetStateVisualId)
    {
        EnsureEditableStructure();
        var transition = GetTransition(transitionVisualId);
        transition.From = GetState(sourceStateVisualId).Name;
        transition.To = GetState(targetStateVisualId).Name;
        RefreshDerivedState();
    }

    public void SetTransitionSource(string transitionVisualId, string sourceStateVisualId)
    {
        EnsureEditableStructure();
        GetTransition(transitionVisualId).From = GetState(sourceStateVisualId).Name;
        RefreshDerivedState();
    }

    public void SetTransitionTarget(string transitionVisualId, string targetStateVisualId)
    {
        EnsureEditableStructure();
        GetTransition(transitionVisualId).To = GetState(targetStateVisualId).Name;
        RefreshDerivedState();
    }

    public void SetTransitionName(string transitionVisualId, string? name)
    {
        EnsureEditableStructure();
        GetTransition(transitionVisualId).Name = NormalizeOptional(name);
        RefreshDerivedState();
    }

    public void SetTransitionDisplayName(string transitionVisualId, string? displayName)
    {
        EnsureEditableStructure();
        GetTransition(transitionVisualId).DisplayName = NormalizeOptional(displayName);
        RefreshDerivedState();
    }

    public void SetStateSlot(string stateVisualId, StateMachineStateSlot slot, JsonNode? value)
    {
        EnsureEditableStructure();
        var state = GetState(stateVisualId);
        var clone = value?.DeepClone();

        if (slot == StateMachineStateSlot.Entry)
            state.Entry = clone;
        else
            state.Exit = clone;

        RefreshDerivedState();
    }

    public void SetTransitionSlot(string transitionVisualId, StateMachineTransitionSlot slot, JsonNode? value)
    {
        EnsureEditableStructure();
        var transition = GetTransition(transitionVisualId);
        var clone = value?.DeepClone();

        switch (slot)
        {
            case StateMachineTransitionSlot.Trigger:
                transition.Trigger = clone;
                break;
            case StateMachineTransitionSlot.Condition:
                transition.Condition = clone;
                break;
            case StateMachineTransitionSlot.Action:
                transition.Action = clone;
                break;
        }

        RefreshDerivedState();
    }

    public void SetStatePosition(string stateVisualId, double x, double y)
    {
        GetState(stateVisualId);
        _statePositions[stateVisualId] = new(x, y);
    }

    public void SetTransitionVertices(string transitionVisualId, IEnumerable<StateMachineCanvasPosition> vertices)
    {
        GetTransition(transitionVisualId);
        _transitionVertices[transitionVisualId] = vertices.ToList();
    }

    /// <summary>
    /// Exports runtime JSON only when the current semantic graph is valid.
    /// </summary>
    public JsonObject Export()
    {
        RefreshDerivedState();
        if (!CanExport)
            throw new InvalidOperationException("Cannot export a StateMachine graph with validation errors.");

        return _mapper.Map(Graph);
    }

    private string RegisterState(StateMachineStateNode state)
    {
        var visualId = $"state-{++_nextStateVisualId}";
        _stateVisualIds[state] = visualId;
        _statesByVisualId[visualId] = state;
        return visualId;
    }

    private string RegisterTransition(StateMachineTransitionEdge transition)
    {
        var visualId = $"transition-{++_nextTransitionVisualId}";
        _transitionVisualIds[transition] = visualId;
        _transitionsByVisualId[visualId] = transition;
        _transitionVertices[visualId] = [];
        return visualId;
    }

    private StateMachineCanvasPosition CreateNextDefaultStatePosition() =>
        new(_nextDefaultStatePositionIndex++ * DefaultHorizontalGap, 0);

    private void RemoveTransition(StateMachineTransitionEdge transition)
    {
        var visualId = _transitionVisualIds[transition];
        Graph.Transitions.Remove(transition);
        _transitionVisualIds.Remove(transition);
        _transitionsByVisualId.Remove(visualId);
        _transitionVertices.Remove(visualId);
    }

    private void EnsureUniqueStateName(string name, StateMachineStateNode? excluded = null)
    {
        if (Graph.States.Any(x => !ReferenceEquals(x, excluded) && string.Equals(x.Name, name, StringComparison.Ordinal)))
            throw new InvalidOperationException($"State name '{name}' is already in use.");
    }

    private void EnsureEditableStructure()
    {
        if (_structuralIssues.Count > 0)
            throw new InvalidOperationException("The StateMachine structure must be repaired before semantic editing.");
    }

    private void RefreshDerivedState()
    {
        var validStateNames = Graph.States
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var state in Graph.States)
        {
            state.IsTerminal = !Graph.Transitions.Any(x =>
                string.Equals(x.From, state.Name, StringComparison.Ordinal) &&
                validStateNames.Contains(x.To));
        }

        Graph.ValidationIssues = _structuralIssues.Concat(_validator.Validate(Graph)).ToList();
    }

    private IReadOnlyCollection<StateMachineValidationIssue> GetStateIssues(StateMachineStateNode state)
    {
        var target = string.IsNullOrWhiteSpace(state.Name) ? "state" : state.Name;
        return Graph.ValidationIssues
            .Where(x => string.Equals(x.Target, target, StringComparison.Ordinal) ||
                        x.Target?.StartsWith($"{target}.", StringComparison.Ordinal) == true)
            .ToList();
    }

    private IReadOnlyCollection<StateMachineValidationIssue> GetTransitionIssues(StateMachineTransitionEdge transition)
    {
        var target = NormalizeOptional(transition.DisplayName)
            ?? NormalizeOptional(transition.Name)
            ?? $"{transition.From}->{transition.To}";
        return Graph.ValidationIssues
            .Where(x => string.Equals(x.Target, target, StringComparison.Ordinal) ||
                        x.Target?.StartsWith($"{target}.", StringComparison.Ordinal) == true)
            .ToList();
    }

    private static bool IsStructuralIssue(StateMachineValidationIssue issue) =>
        issue.Severity == StateMachineValidationSeverity.Error &&
        issue.Code is InvalidStateCollectionCode or InvalidTransitionCollectionCode or InvalidStateItemCode or InvalidTransitionItemCode;

    private static string NormalizeRequiredName(string value, string parameterName)
    {
        var normalized = value?.Trim();
        return !string.IsNullOrWhiteSpace(normalized)
            ? normalized
            : throw new ArgumentException("A non-empty name is required.", parameterName);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

namespace Elsa.Workflows.Attributes;

/// <summary>
/// Type of an input. 
/// To differentiate between state dropdown, conditional input and normal input.
/// </summary>
public enum InputType
{
    Default,
    ConditionalInput,
    StateDropdown
}
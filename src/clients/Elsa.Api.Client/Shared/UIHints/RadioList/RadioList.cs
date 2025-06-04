namespace Elsa.Api.Client.Shared.UIHints.RadioList;

public record RadioList(IEnumerable<RadioListItem> Items, bool IsFlagsEnum = false);
namespace Elsa.Api.Client.Shared.UIHints.CheckList;

public record CheckList(IEnumerable<CheckListItem> Items, bool IsFlagsEnum = false);
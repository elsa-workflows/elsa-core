using System.Collections.Generic;
using System.Linq;

namespace Elsa.Api.Models;

public record ListModel<T>(ICollection<T> Items);

public static class ListModel
{
    public static ListModel<T> Of<T>(ICollection<T> items) => new(items);
    public static ListModel<T> Of<T>(IEnumerable<T> items) => new(items.ToList());
}
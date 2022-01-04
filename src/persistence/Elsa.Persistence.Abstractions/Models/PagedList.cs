using System.Collections.Generic;

namespace Elsa.Persistence.Models;

public record PagedList<T>(ICollection<T> Items, int PageSize, int TotalCount);
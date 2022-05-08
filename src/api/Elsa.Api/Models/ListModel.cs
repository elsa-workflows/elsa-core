using System.Collections.Generic;

namespace Elsa.Api.Models;

public record ListModel<T>(ICollection<T> Items);
using System.Collections.Generic;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;

namespace Elsa.IntegrationTests.Serialization.VariableTypes;

internal record VariablesContainer(ICollection<Variable> Variables);
using System.Collections.Generic;
using Elsa.Workflows.Core.Memory;

namespace Elsa.IntegrationTests.Serialization.VariableTypes;

internal record VariablesContainer(ICollection<Variable> Variables);
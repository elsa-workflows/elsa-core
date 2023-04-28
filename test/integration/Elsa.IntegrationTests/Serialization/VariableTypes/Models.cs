using System.Collections.Generic;
using System.Collections.ObjectModel;
using Elsa.Workflows.Core.Models;

namespace Elsa.IntegrationTests.Serialization.VariableTypes;

internal record VariablesContainer(ICollection<Variable> Variables);
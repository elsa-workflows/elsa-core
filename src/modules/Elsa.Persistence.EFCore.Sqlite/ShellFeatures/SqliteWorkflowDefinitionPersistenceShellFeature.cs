using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures;

/// <summary>
/// Configures the persistence feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Workflow Persistence",
    Description = "Provides Sqlite persistence for workflow definitions and runtime data",
    DependsOn = ["SqliteWorkflowDefinitionPersistence", "SqliteWorkflowInstancePersistence", "SqliteWorkflowRuntimePersistence"])]
[UsedImplicitly]
public class SqliteWorkflowPersistenceShellFeature;
namespace Elsa.Workflows.Runtime;

/// <summary>
/// Marks a workflows provider whose workflows do not depend on the current tenant and do not change while the
/// application runs.
/// </summary>
/// <remarks>
/// The workflows of such a provider may be built once per service container instead of once per tenant.
/// </remarks>
internal interface ITenantAgnosticWorkflowsProvider : IWorkflowsProvider;

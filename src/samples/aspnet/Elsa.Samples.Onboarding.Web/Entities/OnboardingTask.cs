namespace Elsa.Samples.Onboarding.Web.Entities;

/// <summary>
/// A task that needs to be completed by the user.
/// </summary>
public class OnboardingTask
{
    /// <summary>
    /// The ID of the task.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// An external ID that can be used to reference the task.
    /// </summary>
    public string ExternalId { get; set; } = default!;

    /// <summary>
    /// The ID of the onboarding process that the task belongs to.
    /// </summary>
    public string ProcessId { get; set; } = default!;

    /// <summary>
    /// The name of the task.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// The task description.
    /// </summary>
    public string Description { get; set; } = default!;

    /// <summary>
    /// The name of the employee being onboarded.
    /// </summary>
    public string EmployeeName { get; set; } = default!;

    /// <summary>
    /// The email address of the employee being onboarded.
    /// </summary>
    public string EmployeeEmail { get; set; } = default!;

    /// <summary>
    /// Whether the task has been completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// The date and time when the task was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the task was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
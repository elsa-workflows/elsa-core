// using Elsa.Api.Client.Activities;
// using Elsa.Api.Client.Extensions;
// using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
//
// namespace Elsa.Api.Client.Shared.Models;
//
// /// <summary>
// /// Represents a container activity.
// /// </summary>
// public class Container : Activity
// {
//     /// <summary>
//     /// Gets or sets the activities contained in this container.
//     /// </summary>
//     public ICollection<Activity> Activities
//     {
//         get => this.TryGetValue<ICollection<Activity>>("activities", () => new List<Activity>())!;
//         set => this["activities"] = value;
//     }
//
//     /// <summary>
//     /// Gets or sets the variables in this container.
//     /// </summary>
//     public ICollection<Variable> Variables
//     {
//         get => this.TryGetValue<ICollection<Variable>>("variables", () => new List<Variable>())!;
//         set => this["variables"] = value;
//     }
// }
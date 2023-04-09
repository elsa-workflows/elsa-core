// namespace Elsa.Jobs.Activities.Attributes;
//
// [AttributeUsage(AttributeTargets.Class)]
// public class JobAttribute : Attribute
// {
//     /// <inheritdoc />
//     public JobAttribute(string @namespace, string? category, string? description = default)
//     {
//         Namespace = @namespace;
//         Description = description;
//         Category = category;
//     }
//
//     /// <inheritdoc />
//     public JobAttribute(string @namespace, string? description = default)
//     {
//         Namespace = @namespace;
//         Description = description;
//         Category = @namespace;
//     }
//
//     /// <inheritdoc />
//     public JobAttribute(string @namespace, string? activityType, string? description = default, string? category = default)
//     {
//         Namespace = @namespace;
//         ActivityType = activityType;
//         Description = description;
//         Category = category;
//     }
//
//     public string? Namespace { get; set;}
//     public string? ActivityType { get; set;}
//     public string? Description { get; set;}
//     public string? DisplayName { get; set; }
//     public string? Category { get; set;}
// }
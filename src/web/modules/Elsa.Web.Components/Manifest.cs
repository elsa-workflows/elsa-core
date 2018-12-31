using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Workflows View Components",
    Category = "Workflows",
    Description = "Provides a set of reusable view components such as the Workflow Editor.",
    Dependencies = new[] { "OrchardCore.Resources" }
)]
using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Workflows Management",
    Category = "Workflows",
    Description = "Provides admin screens to manage workflows.",
    Dependencies = new[] { "Elsa.Web.Components" }
)]
using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Workflows Management",
    Category = "Elsa Workflows",
    Description = "Provides admin screens to manage workflows.",
    Dependencies = new[] { "Elsa.Web.BootstrapTheme.Web.Components" }
)]
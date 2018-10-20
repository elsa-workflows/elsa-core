using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Flowsharp Workflows Management",
    Description = "Provides admin screens to manage workflows.",
    Dependencies = new[]{ "Flowsharp.Web.ViewComponents" }
)]

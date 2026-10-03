namespace Elsa.Studio.Models;

/// <summary>A page the navigation leads to, with its href relative to the app's base address.</summary>
public record MenuPage(string Href, string Text, string? Icon);

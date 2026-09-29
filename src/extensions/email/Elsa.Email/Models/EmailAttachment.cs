namespace Elsa.Email.Models;

/// <summary>
/// Represents an email attachment.
/// </summary>
/// <param name="Content">The content of the attachment.</param>
/// <param name="FileName">The name of the attachment.</param>
/// <param name="ContentType">The content type of the attachment.</param>
public record EmailAttachment(object Content, string? FileName, string? ContentType);
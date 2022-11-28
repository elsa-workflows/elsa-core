namespace Elsa.Email.Models
{
    public record EmailAttachment(object Content, string? FileName, string? ContentType);
}
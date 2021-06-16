// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Email
{
    public record EmailAttachment(byte[] Content, string? FileName, string? ContentType);
}
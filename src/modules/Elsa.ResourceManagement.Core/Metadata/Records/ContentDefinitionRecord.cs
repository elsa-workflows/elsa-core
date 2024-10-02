namespace Elsa.ResourceManagement.Metadata.Records;

public class ContentDefinitionRecord : Document
{
    public ContentDefinitionRecord()
    {
        ContentTypeDefinitionRecords = [];
        ContentPartDefinitionRecords = [];
    }

    public IList<ContentTypeDefinitionRecord> ContentTypeDefinitionRecords { get; set; }
    public IList<ContentPartDefinitionRecord> ContentPartDefinitionRecords { get; set; }
}
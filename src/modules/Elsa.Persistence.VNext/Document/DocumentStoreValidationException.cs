namespace Elsa.Persistence.VNext.Document;

public class DocumentStoreValidationException(string message) : InvalidOperationException(message);

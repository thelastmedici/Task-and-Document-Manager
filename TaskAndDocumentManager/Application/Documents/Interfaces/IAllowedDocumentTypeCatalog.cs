namespace TaskAndDocumentManager.Application.Documents.Interfaces;

public interface IAllowedDocumentTypeCatalog
{
    IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetAllowedTypes();
    bool IsAllowedExtension(string extension);
    bool IsAllowedContentType(string extension, string contentType);
}

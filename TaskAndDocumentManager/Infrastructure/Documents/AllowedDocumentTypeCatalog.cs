using System.Collections.ObjectModel;
using Microsoft.Extensions.Caching.Memory;
using TaskAndDocumentManager.Application.Documents.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Documents;

public sealed class AllowedDocumentTypeCatalog : IAllowedDocumentTypeCatalog
{
    private const string CacheKey = "documents.allowed-types.v1";

    private readonly IMemoryCache _cache;

    public AllowedDocumentTypeCatalog(IMemoryCache cache)
    {
        _cache = cache;
    }

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetAllowedTypes()
    {
        return _cache.GetOrCreate(
            CacheKey,
            entry =>
            {
                entry.Priority = CacheItemPriority.NeverRemove;

                var allowedTypes = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    [".pdf"] = Array.AsReadOnly(new[] { "application/pdf" }),
                    [".png"] = Array.AsReadOnly(new[] { "image/png" }),
                    [".jpg"] = Array.AsReadOnly(new[] { "image/jpeg" }),
                    [".jpeg"] = Array.AsReadOnly(new[] { "image/jpeg" }),
                    [".docx"] = Array.AsReadOnly(new[]
                    {
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                    })
                };

                return new ReadOnlyDictionary<string, IReadOnlyCollection<string>>(allowedTypes);
            })!;
    }

    public bool IsAllowedExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return GetAllowedTypes().ContainsKey(extension.Trim());
    }

    public bool IsAllowedContentType(string extension, string contentType)
    {
        if (string.IsNullOrWhiteSpace(extension) || string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return GetAllowedTypes().TryGetValue(extension.Trim(), out var allowedContentTypes) &&
            allowedContentTypes.Contains(contentType.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}

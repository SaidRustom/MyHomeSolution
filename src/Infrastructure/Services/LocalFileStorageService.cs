using MyHomeSolution.Application.Common.Interfaces;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class LocalFileStorageService(string basePath) : IFileStorageService
{
    private static readonly Dictionary<string, string> ContentTypeMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".pdf"] = "application/pdf"
    };

    public async Task<string> UploadAsync(
        string containerName, string fileName, Stream content, string contentType,
        CancellationToken cancellationToken = default)
    {
        var directoryPath = Path.Combine(basePath, containerName, Path.GetDirectoryName(fileName) ?? string.Empty);
        Directory.CreateDirectory(directoryPath);

        var filePath = Path.Combine(basePath, containerName, fileName);
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fileStream, cancellationToken);

        return $"/{containerName}/{fileName}";
    }

    public Task<(Stream Content, string ContentType)?> DownloadAsync(
        string containerName, string fileName,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(basePath, containerName, fileName);
        if (!File.Exists(filePath))
            return Task.FromResult<(Stream Content, string ContentType)?>(null);

        var extension = Path.GetExtension(filePath);
        var contentType = ContentTypeMappings.GetValueOrDefault(extension, "application/octet-stream");
        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        return Task.FromResult<(Stream Content, string ContentType)?>((stream, contentType));
    }

    public Task DeleteAsync(
        string containerName, string fileName,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(basePath, containerName, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}

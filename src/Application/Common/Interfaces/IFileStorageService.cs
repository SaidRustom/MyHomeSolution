namespace MyHomeSolution.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        string containerName, string fileName, Stream content, string contentType,
        CancellationToken cancellationToken = default);

    Task<(Stream Content, string ContentType)?> DownloadAsync(
        string containerName, string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string containerName, string fileName,
        CancellationToken cancellationToken = default);
}

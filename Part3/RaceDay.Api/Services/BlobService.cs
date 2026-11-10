using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
namespace RaceDay.Api.Services;

public class BlobService
{
    readonly BlobContainerClient _c;
    public BlobService(IConfiguration cfg) =>
        _c = new BlobContainerClient(cfg["Blob:ConnectionString"], cfg["Blob:Container"]);

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        await _c.CreateIfNotExistsAsync(PublicAccessType.Blob);
        var name = $"{folder}/{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLower()}";
        var blob = _c.GetBlobClient(name);
        await using var s = file.OpenReadStream();
        await blob.UploadAsync(s, new BlobHttpHeaders { ContentType = file.ContentType });
        return blob.Uri.ToString();
    }
}
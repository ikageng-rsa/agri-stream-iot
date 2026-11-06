using AgriStream.Models;
using AgriStream.Shared;
using AgriStream.ViewModels;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriStream.Controllers;

public class ImagesController : Controller
{
    private const long MaxUploadBytes = 100 * 1024 * 1024;

    private readonly BlobContainerClient _container;

    public ImagesController(BlobServiceClient blobServiceClient)
    {
        _container = blobServiceClient.GetBlobContainerClient(StorageNames.DroneImagesContainer);
        _container.CreateIfNotExists();
    }

    public async Task<IActionResult> Index()
    {
        var items = new List<DroneImageViewModel>();
        await foreach (var blob in _container.GetBlobsAsync(new GetBlobsOptions { Traits = BlobTraits.Metadata }))
        {
            blob.Metadata.TryGetValue("fieldId", out var fieldId);
            blob.Metadata.TryGetValue("uploadedBy", out var uploadedBy);
            items.Add(new DroneImageViewModel
            {
                Name = blob.Name,
                FieldId = fieldId,
                UploadedBy = uploadedBy,
                SizeBytes = blob.Properties.ContentLength ?? 0,
                LastModified = blob.Properties.LastModified
            });
        }

        return View(items.OrderByDescending(b => b.LastModified).ToList());
    }

    [Authorize(Roles = $"{Roles.Farmer},{Roles.Admin}")]
    public IActionResult Upload() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{Roles.Farmer},{Roles.Admin}")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<IActionResult> Upload(IFormFile file, string fieldId)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select an image file to upload.");
            return View();
        }

        if (string.IsNullOrWhiteSpace(fieldId))
        {
            ModelState.AddModelError(string.Empty, "Please enter the field the image was captured over.");
            return View();
        }

        var blobName = $"{fieldId.Trim().ToUpperInvariant()}/{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var blobClient = _container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType },
            Metadata = new Dictionary<string, string>
            {
                ["fieldId"] = fieldId.Trim().ToUpperInvariant(),
                ["uploadedBy"] = User.Identity?.Name ?? "unknown",
                ["originalName"] = Path.GetFileName(file.FileName)
            }
        });

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> GetImage(string name)
    {
        var blobClient = _container.GetBlobClient(name);
        if (!await blobClient.ExistsAsync())
        {
            return NotFound();
        }

        var download = await blobClient.DownloadStreamingAsync();
        var contentType = download.Value.Details.ContentType ?? "application/octet-stream";
        return File(download.Value.Content, contentType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(string name)
    {
        await _container.GetBlobClient(name).DeleteIfExistsAsync();
        return RedirectToAction(nameof(Index));
    }
}

using AgriStream.Models;
using AgriStream.Shared;
using AgriStream.Shared.Models;
using Azure.Data.Tables;
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
    private readonly TableClient _auditTable;

    public ImagesController(BlobServiceClient blobServiceClient, TableServiceClient tableServiceClient)
    {
        _container = blobServiceClient.GetBlobContainerClient(StorageNames.DroneImagesContainer);
        _container.CreateIfNotExists();
        _auditTable = tableServiceClient.GetTableClient(StorageNames.ImageAuditTable);
        _auditTable.CreateIfNotExists();
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

        // Audit rows are written by the LogDroneImageUpload blob-triggered function.
        var audits = new Dictionary<string, ImageAudit>();
        await foreach (var audit in _auditTable.QueryAsync<ImageAudit>(a => a.PartitionKey == "DroneImage"))
        {
            audits[audit.BlobName] = audit;
        }

        foreach (var item in items)
        {
            if (audits.TryGetValue(item.Name, out var audit))
            {
                item.AuditStatus = audit.Status;
                item.AuditedAt = audit.ProcessedAt;
            }
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

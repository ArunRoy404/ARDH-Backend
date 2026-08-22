using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using CleanArchitecture.Application.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace CleanArchitecture.Web.Controller;

[Authorize] 
[ApiController]
[Route("api/upload")]
public class UploadController(AppSettings appSettings, IWebHostEnvironment environment) : BaseController
{
    private readonly AppSettings _appSettings = appSettings;
    private readonly IWebHostEnvironment _environment = environment;

    /// <summary>
    /// [F-01] Upload image file (PNG, JPG, WEBP)
    /// </summary>
    [HttpPost("image")]
    [SwaggerOperation(Summary = "[F-01] Upload image file (PNG, JPG, WEBP)")]
    [SwaggerResponse(200, "Image uploaded successfully.")]
    [SwaggerResponse(400, "Invalid image format or size.")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = $"Invalid image format. Allowed formats: {string.Join(", ", allowedExtensions)}" });
        }

        var url = await SaveFileAsync(file, extension, _appSettings.FileStorageSettings.ImagePath ?? "image");
        return Ok(new { url });
    }

    /// <summary>
    /// [F-02] Upload document file (PDF, DOC, etc.)
    /// </summary>
    [HttpPost("document")]
    [SwaggerOperation(Summary = "[F-02] Upload document file (PDF, DOC, etc.)")]
    [SwaggerResponse(200, "Document uploaded successfully.")]
    [SwaggerResponse(400, "Invalid document format.")]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = $"Invalid document format. Allowed formats: {string.Join(", ", allowedExtensions)}" });
        }

        var url = await SaveFileAsync(file, extension, _appSettings.FileStorageSettings.DocumentPath ?? "document");
        return Ok(new { url });
    }

    /// <summary>
    /// [F-05] Upload XLSX file (for bulk uploads)
    /// </summary>
    [HttpPost("xlsx")]
    [SwaggerOperation(Summary = "[F-05] Upload XLSX file (for bulk uploads)")]
    [SwaggerResponse(200, "XLSX file uploaded successfully.")]
    [SwaggerResponse(400, "Invalid XLSX format or size.")]
    public async Task<IActionResult> UploadXlsx(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        var allowedExtensions = new[] { ".xlsx" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Invalid file format. Only .xlsx files are allowed." });
        }

        var url = await SaveFileAsync(file, extension, _appSettings.FileStorageSettings.BulkUploadPath ?? "bulk-upload");
        return Ok(new { url });
    }

    /// <summary>
    /// [F-03] Upload tenant ID proof document (PDF, DOC, etc.)
    /// </summary>
    [HttpPost("id-proof")]
    [SwaggerOperation(Summary = "[F-03] Upload tenant ID proof document (PDF, DOC, etc.)")]
    [SwaggerResponse(200, "ID proof uploaded successfully.")]
    [SwaggerResponse(400, "Invalid document format.")]
    public async Task<IActionResult> UploadIdProof(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = $"Invalid document format. Allowed formats: {string.Join(", ", allowedExtensions)}" });
        }

        var url = await SaveFileAsync(file, extension, _appSettings.FileStorageSettings.DocumentPath ?? "document");
        return Ok(new { url });
    }

    /// <summary>
    /// [F-04] Delete uploaded file
    /// </summary>
    [HttpDelete("{fileId}")]
    [SwaggerOperation(Summary = "[F-04] Delete uploaded file")]
    [SwaggerResponse(200, "File deleted successfully.")]
    [SwaggerResponse(404, "File not found.")]
    public IActionResult DeleteFile(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return BadRequest(new { message = "Invalid file ID." });
        }

        var safeFileId = Path.GetFileName(fileId);
        if (string.IsNullOrWhiteSpace(safeFileId))
        {
            return BadRequest(new { message = "Invalid file ID." });
        }

        var folders = new[]
        {
            _appSettings.FileStorageSettings.ImagePath ?? "image",
            _appSettings.FileStorageSettings.DocumentPath ?? "document",
            _appSettings.FileStorageSettings.BulkUploadPath ?? "bulk-upload",
            _appSettings.FileStorageSettings.Path ?? "image"
        };

        var deleted = false;
        foreach (var folder in folders.Distinct())
        {
            var storagePath = Path.Combine(_environment.ContentRootPath, folder.Trim('/'));
            if (!Directory.Exists(storagePath))
            {
                continue;
            }

            var files = Directory.GetFiles(storagePath, $"{safeFileId}.*");
            if (files.Length > 0)
            {
                foreach (var filePath in files)
                {
                    System.IO.File.Delete(filePath);
                }
                deleted = true;
            }
            else
            {
                var exactFile = Path.Combine(storagePath, safeFileId);
                if (System.IO.File.Exists(exactFile))
                {
                    System.IO.File.Delete(exactFile);
                    deleted = true;
                }
            }
        }

        if (deleted)
        {
            return Ok(new { message = "File deleted successfully." });
        }

        return NotFound(new { message = "File not found." });
    }

    private async Task<string> SaveFileAsync(IFormFile file, string extension, string folderName)
    {
        var fileId = Guid.NewGuid().ToString();
        var fileName = $"{fileId}{extension}";
        var cleanFolder = folderName.Trim('/');
        var storagePath = Path.Combine(_environment.ContentRootPath, cleanFolder);

        if (!Directory.Exists(storagePath))
        {
            Directory.CreateDirectory(storagePath);
        }

        var filePath = Path.Combine(storagePath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var baseUrl = !string.IsNullOrWhiteSpace(_appSettings.BaseURL)
            ? _appSettings.BaseURL.TrimEnd('/')
            : _appSettings.AppUrl?.TrimEnd('/') ?? string.Empty;

        return $"{baseUrl}/{cleanFolder}/{fileName}";
    }
}

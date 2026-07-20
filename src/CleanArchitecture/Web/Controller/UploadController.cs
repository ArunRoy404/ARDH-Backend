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

        var url = await SaveFileAsync(file, extension);
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

        var url = await SaveFileAsync(file, extension);
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

        var url = await SaveFileAsync(file, extension);
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

        var storagePath = Path.Combine(_environment.ContentRootPath, _appSettings.FileStorageSettings.Path);
        if (!Directory.Exists(storagePath))
        {
            return NotFound(new { message = "File not found." });
        }

        // Find file starting with fileId
        var files = Directory.GetFiles(storagePath, $"{fileId}.*");
        if (files.Length == 0)
        {
            // Also search for exact file if fileId includes extension
            var exactFile = Path.Combine(storagePath, fileId);
            if (System.IO.File.Exists(exactFile))
            {
                System.IO.File.Delete(exactFile);
                return Ok(new { message = "File deleted successfully." });
            }

            return NotFound(new { message = "File not found." });
        }

        foreach (var filePath in files)
        {
            System.IO.File.Delete(filePath);
        }

        return Ok(new { message = "File deleted successfully." });
    }

    private async Task<string> SaveFileAsync(IFormFile file, string extension)
    {
        var fileId = Guid.NewGuid().ToString();
        var fileName = $"{fileId}{extension}";
        var storagePath = Path.Combine(_environment.ContentRootPath, _appSettings.FileStorageSettings.Path);

        if (!Directory.Exists(storagePath))
        {
            Directory.CreateDirectory(storagePath);
        }

        var filePath = Path.Combine(storagePath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var appUrl = _appSettings.AppUrl?.TrimEnd('/') ?? string.Empty;
        var folderName = _appSettings.FileStorageSettings.Path.Trim('/');
        return $"{appUrl}/{folderName}/{fileName}";
    }
}

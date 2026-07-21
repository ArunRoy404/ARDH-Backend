using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Income;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/income")]
public class IncomeController(IIncomeRecordService incomeRecordService, IUnitOfWork unitOfWork) : BaseController
{
    private readonly IIncomeRecordService _incomeRecordService = incomeRecordService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// [I-01] Retrieves all income records with support for pagination, search, and filters.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of income records retrieved successfully.", typeof(PaginatedList<IncomeRecordViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<PaginatedList<IncomeRecordViewModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] IncomeType? incomeType = null,
        [FromQuery] IncomeStatus? status = null,
        [FromQuery] Guid? buildingId = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _incomeRecordService.GetPaginated(
            page, pageSize, search, incomeType, status, buildingId, tenantId, startDate, endDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [I-02] Retrieves single income record details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [SwaggerResponse(200, "Income record details retrieved successfully.", typeof(IncomeRecordViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Income record not found.")]
    public async Task<ActionResult<IncomeRecordViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _incomeRecordService.GetById(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [I-03] Creates a new income record.
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "Income record created successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> Create([FromBody] IncomeRecordCreateRequest request, CancellationToken cancellationToken)
    {
        await _incomeRecordService.Create(request, cancellationToken);
        return Ok(new { message = "Income record created successfully." });
    }

    /// <summary>
    /// [I-04] Updates details of an existing income record.
    /// </summary>
    [HttpPut("{id:guid}")]
    [SwaggerResponse(200, "Income record updated successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Income record not found.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] IncomeRecordUpdateRequest request, CancellationToken cancellationToken)
    {
        await _incomeRecordService.Update(id, request, cancellationToken);
        return Ok(new { message = "Income record updated successfully." });
    }

    /// <summary>
    /// [I-05] Soft-deletes an income record.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [SwaggerResponse(200, "Income record deleted successfully.")]
    [SwaggerResponse(400, "Invalid Admin password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Income record not found.")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(password))
        {
            if (HttpContext.Request.Headers.TryGetValue("X-Admin-Password", out var headerPassword))
            {
                password = headerPassword.ToString();
            }
        }

        var settings = await _unitOfWork.SettingRepository.FirstOrDefaultAsync(x => true);
        if (settings == null || string.IsNullOrEmpty(password) || !CleanArchitecture.Application.Common.Utilities.StringHelper.Verify(password, settings.AdminPassword))
        {
            return BadRequest(new { message = "Invalid Admin password." });
        }

        await _incomeRecordService.Delete(id, cancellationToken);
        return Ok(new { message = "Income record deleted successfully." });
    }

    /// <summary>
    /// [I-06] Updates the status of an income record.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [SwaggerResponse(200, "Income record status updated successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Income record not found.")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] IncomeRecordStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        await _incomeRecordService.UpdateStatus(id, request, cancellationToken);
        return Ok(new { message = "Income status updated successfully." });
    }

    /// <summary>
    /// [I-07] Downloads the PDF receipt for a specific income record.
    /// </summary>
    [HttpGet("download/{id:guid}")]
    [SwaggerResponse(200, "PDF receipt generated and downloaded successfully.", typeof(FileResult))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Income record not found.")]
    public async Task<IActionResult> DownloadReceipt(Guid id, CancellationToken cancellationToken)
    {
        var bytes = await _incomeRecordService.GenerateReceiptPdf(id, cancellationToken);
        return File(bytes, "application/pdf", $"receipt_{id}.pdf");
    }

    /// <summary>
    /// [I-08] Exports all filtered income records to a CSV file.
    /// </summary>
    [HttpGet("download-csv")]
    [SwaggerResponse(200, "CSV file exported and downloaded successfully.", typeof(FileResult))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> DownloadCsv(
        [FromQuery] string? search = null,
        [FromQuery] IncomeType? incomeType = null,
        [FromQuery] IncomeStatus? status = null,
        [FromQuery] Guid? buildingId = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var bytes = await _incomeRecordService.ExportToCsv(
            search, incomeType, status, buildingId, tenantId, startDate, endDate, cancellationToken);
        return File(bytes, "text/csv", "income_records.csv");
    }
}

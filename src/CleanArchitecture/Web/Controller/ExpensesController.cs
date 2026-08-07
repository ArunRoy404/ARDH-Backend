using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/expenses")]
public class ExpensesController(IExpenseRecordService expenseRecordService, IUnitOfWork unitOfWork) : BaseController
{
    private readonly IExpenseRecordService _expenseRecordService = expenseRecordService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// [EX-01] Retrieves all expense records with support for pagination, search, and filters.
    /// </summary>
    /// <remarks>
    /// Filter values (exact):
    ///   category = Utility | Operational | Maintenance | Tax | Capital
    ///   status = Draft | PendingPayment | Paid
    ///   nature = Service | Material | Others
    /// </remarks>
    [HttpGet]
    [SwaggerResponse(200, "List of expense records retrieved successfully.", typeof(PaginatedList<ExpenseRecordViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<PaginatedList<ExpenseRecordViewModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] ExpenseCategory? category = null,
        [FromQuery] ExpenseStatus? status = null,
        [FromQuery] ExpenseNature? nature = null,
        [FromQuery] Guid? buildingId = null,
        [FromQuery] Guid? vendorId = null,
        [FromQuery] Guid? apartmentId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _expenseRecordService.GetPaginated(
            page, pageSize, search, category, status, nature, buildingId, vendorId, apartmentId, startDate, endDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [EX-02] Retrieves single expense record details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [SwaggerResponse(200, "Expense record details retrieved successfully.", typeof(ExpenseRecordViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Expense record not found.")]
    public async Task<ActionResult<ExpenseRecordViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _expenseRecordService.GetById(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [EX-03] Creates a new expense record.
    /// </summary>
    /// <remarks>
    /// Mandatory fields: expenseHead, specificItem, nature, amount, entity, buildingId, expenseDate, paymentMethod and status.
    /// For ApartmentSpecific entity, apartmentId is also mandatory.
    /// Valid enum values (exact):
    ///   category = Utility | Operational | Maintenance | Tax | Capital (optional)
    ///   nature = Service | Material | Others
    ///   entity = General | ApartmentSpecific | BuildingLevel
    ///   status = Draft | PendingPayment | Paid
    /// paymentMethod is a free-form text value (for example: Cash, BankTransfer, Cheque, UPI, Card, Online).
    /// Rules:
    ///   - Amount must be greater than 0.
    ///   - Expense date cannot be in the future.
    ///   - A duplicate entry (same amount + expense date + expense head + specific item + nature) is rejected.
    ///   - For water tank deliveries, tankerNumber, timeOfDelivery, deliveryDriverName, managerInAttendance and litersFilled
    ///     must be provided, and the same tanker number at the same delivery time is rejected.
    /// </remarks>
    [HttpPost]
    [SwaggerResponse(200, "Expense record created successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> Create([FromBody] ExpenseRecordCreateRequest request, CancellationToken cancellationToken)
    {
        await _expenseRecordService.Create(request, cancellationToken);
        return Ok(new { message = "Expense record created successfully." });
    }

    /// <summary>
    /// [EX-04] Updates details of an existing expense record.
    /// </summary>
    /// <remarks>
    /// Mandatory fields: expenseHead, specificItem, nature, amount, entity, buildingId, expenseDate, paymentMethod and status.
    /// For ApartmentSpecific entity, apartmentId is also mandatory.
    /// Valid enum values (exact):
    ///   category = Utility | Operational | Maintenance | Tax | Capital (optional)
    ///   nature = Service | Material | Others
    ///   entity = General | ApartmentSpecific | BuildingLevel
    ///   status = Draft | PendingPayment | Paid
    /// paymentMethod is a free-form text value (for example: Cash, BankTransfer, Cheque, UPI, Card, Online).
    /// Rules:
    ///   - Amount must be greater than 0.
    ///   - Expense date cannot be in the future.
    ///   - A duplicate entry (same amount + expense date + expense head + specific item + nature) is rejected.
    ///   - For water tank deliveries, tankerNumber, timeOfDelivery, deliveryDriverName, managerInAttendance and litersFilled
    ///     must be provided, and the same tanker number at the same delivery time is rejected.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [SwaggerResponse(200, "Expense record updated successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Expense record not found.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ExpenseRecordUpdateRequest request, CancellationToken cancellationToken)
    {
        await _expenseRecordService.Update(id, request, cancellationToken);
        return Ok(new { message = "Expense record updated successfully." });
    }

    /// <summary>
    /// [EX-05] Soft-deletes an expense record.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [SwaggerResponse(200, "Expense record deleted successfully.")]
    [SwaggerResponse(400, "Invalid Admin password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Expense record not found.")]
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

        await _expenseRecordService.Delete(id, cancellationToken);
        return Ok(new { message = "Expense record deleted successfully." });
    }

    /// <summary>
    /// [EX-06] Updates the status of an expense record.
    /// </summary>
    /// <remarks>
    /// Valid status values (exact): Draft | PendingPayment | Paid
    /// </remarks>
    [HttpPatch("{id:guid}/status")]
    [SwaggerResponse(200, "Expense record status updated successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Expense record not found.")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] ExpenseRecordStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        await _expenseRecordService.UpdateStatus(id, request, cancellationToken);
        return Ok(new { message = "Expense status updated successfully." });
    }

    /// <summary>
    /// [EX-07] Exports all filtered expense records to a CSV file.
    /// </summary>
    /// <remarks>
    /// Filter values (exact):
    ///   category = Utility | Operational | Maintenance | Tax | Capital
    ///   status = Draft | PendingPayment | Paid
    ///   nature = Service | Material | Others
    /// </remarks>
    [HttpGet("download-csv")]
    [SwaggerResponse(200, "CSV file exported and downloaded successfully.", typeof(FileResult))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> DownloadCsv(
        [FromQuery] string? search = null,
        [FromQuery] ExpenseCategory? category = null,
        [FromQuery] ExpenseStatus? status = null,
        [FromQuery] ExpenseNature? nature = null,
        [FromQuery] Guid? buildingId = null,
        [FromQuery] Guid? vendorId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var bytes = await _expenseRecordService.ExportToCsv(
            search, category, status, nature, buildingId, vendorId, startDate, endDate, cancellationToken);
        return File(bytes, "text/csv", "expense_records.csv");
    }
}

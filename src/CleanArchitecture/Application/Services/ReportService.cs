using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Income;
using CleanArchitecture.Shared.Models.Expenses;
using CleanArchitecture.Shared.Models.Reports;

namespace CleanArchitecture.Application.Services;

public class ReportService(
    IUnitOfWork unitOfWork,
    IIncomeRecordService incomeRecordService,
    IExpenseRecordService expenseRecordService) : IReportService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IIncomeRecordService _incomeRecordService = incomeRecordService;
    private readonly IExpenseRecordService _expenseRecordService = expenseRecordService;

    public Task<PaginatedList<IncomeRecordViewModel>> GetIncomeReport(
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => _incomeRecordService.GetPaginated(
            page,
            pageSize,
            search: null,
            incomeType: null,
            status: null,
            buildingId: buildingId,
            apartmentId: null,
            startDate: startDate,
            endDate: endDate,
            cancellationToken: cancellationToken);

    public Task<PaginatedList<ExpenseRecordViewModel>> GetExpenseReport(
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => _expenseRecordService.GetPaginated(
            page,
            pageSize,
            search: null,
            category: null,
            status: null,
            nature: null,
            buildingId: buildingId,
            vendorId: null,
            apartmentId: null,
            startDate: startDate,
            endDate: endDate,
            cancellationToken: cancellationToken);

    public async Task<ReportStatsViewModel> GetReportStats(
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;

        var incomes = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x =>
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            (!startDate.HasValue || x.PaymentDate >= startDate.Value) &&
            (!endDate.HasValue || x.PaymentDate <= endDate.Value) &&
            x.Status == IncomeStatus.Paid);

        var expenses = await _unitOfWork.ExpenseRecordRepository.GetAllAsync(x =>
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            (!startDate.HasValue || x.ExpenseDate >= startDate.Value) &&
            (!endDate.HasValue || x.ExpenseDate <= endDate.Value) &&
            x.Status == ExpenseStatus.Paid);

        var totalIncomes = incomes.Sum(x => x.Amount);
        var totalExpenses = expenses.Sum(x => x.Amount);

        return new ReportStatsViewModel
        {
            TotalIncomes = totalIncomes,
            TotalExpenses = totalExpenses,
            Net = totalIncomes - totalExpenses
        };
    }

    public async Task<byte[]> ExportReportToXlsx(
        string type,
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var typeLower = type?.Trim().ToLower() ?? "combined";

        if (typeLower == "income")
        {
            return await _incomeRecordService.ExportToXlsx(
                search: null,
                incomeType: null,
                status: null,
                buildingId: buildingId,
                startDate: startDate,
                endDate: endDate,
                cancellationToken: cancellationToken);
        }

        if (typeLower is "expense" or "expenses")
        {
            return await _expenseRecordService.ExportToXlsx(
                search: null,
                category: null,
                status: null,
                nature: null,
                buildingId: buildingId,
                vendorId: null,
                startDate: startDate,
                endDate: endDate,
                cancellationToken: cancellationToken);
        }

        if (typeLower != "combined")
        {
            throw ReportException.BadRequestException(
                $"Invalid report type '{type}'. Valid types: income, expenses, combined.");
        }

        // Combined transaction ledger merging income + expense into one dated feed
        var hasBuildingFilter = buildingId.HasValue && buildingId.Value != Guid.Empty;

        var incomeRecords = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x =>
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            (!startDate.HasValue || x.PaymentDate >= startDate.Value) &&
            (!endDate.HasValue || x.PaymentDate <= endDate.Value));

        var expenseRecords = await _unitOfWork.ExpenseRecordRepository.GetAllAsync(x =>
            (!hasBuildingFilter || x.BuildingId == buildingId!.Value) &&
            (!startDate.HasValue || x.ExpenseDate >= startDate.Value) &&
            (!endDate.HasValue || x.ExpenseDate <= endDate.Value));

        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);

        var ledgerItems = new List<LedgerItem>();

        foreach (var inc in incomeRecords)
        {
            ledgerItems.Add(new LedgerItem
            {
                Date = inc.PaymentDate,
                Type = "Income",
                Category = inc.IncomeType.ToString(),
                Description = $"Received from {inc.IncomeEntity}",
                Amount = inc.Amount,
                BuildingId = inc.BuildingId,
                ApartmentId = inc.ApartmentId,
                Status = inc.Status.ToString()
            });
        }

        foreach (var exp in expenseRecords)
        {
            ledgerItems.Add(new LedgerItem
            {
                Date = exp.ExpenseDate,
                Type = "Expense",
                Category = exp.Category.ToString(),
                Description = string.IsNullOrEmpty(exp.SpecificItem) ? exp.ExpenseHead ?? "Expense" : exp.SpecificItem,
                Amount = -exp.Amount, // negative for expense
                BuildingId = exp.BuildingId,
                ApartmentId = exp.ApartmentId,
                Status = exp.Status.ToString()
            });
        }

        var list = ledgerItems.OrderByDescending(x => x.Date).ToList();

        var rows = new List<List<string>>
        {
            new() { "Date", "Type", "Category", "Description", "Amount", "Status", "BuildingName", "FlatNumber" }
        };

        foreach (var r in list)
        {
            var bName = r.BuildingId.HasValue && buildingMap.TryGetValue(r.BuildingId.Value, out var bn) ? bn : string.Empty;
            var fNum = r.ApartmentId.HasValue && apartmentMap.TryGetValue(r.ApartmentId.Value, out var fn) ? fn : string.Empty;

            rows.Add(new List<string>
            {
                r.Date.ToString("yyyy-MM-dd"),
                r.Type,
                r.Category,
                r.Description,
                r.Amount.ToString("F2"),
                r.Status,
                bName,
                fNum
            });
        }

        return CleanArchitecture.Application.Common.Utilities.XlsxHelper.BuildXlsx(rows, "Report");
    }

    private class LedgerItem
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public Guid? BuildingId { get; set; }
        public Guid? ApartmentId { get; set; }
    }
}

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
        // Only needs Amount/Status/BuildingId/date per record — no building/apartment/vendor
        // name lookups, so a lean direct repository fetch stays leaner than routing through
        // GetPaginated (which would build full ViewModels just to be summed and discarded).
        var incomes = await _unitOfWork.IncomeRecordRepository.GetAllAsync();
        var expenses = await _unitOfWork.ExpenseRecordRepository.GetAllAsync();

        var incomeQuery = incomes.AsQueryable();
        var expenseQuery = expenses.AsQueryable();

        if (buildingId.HasValue)
        {
            incomeQuery = incomeQuery.Where(x => x.BuildingId == buildingId.Value);
            expenseQuery = expenseQuery.Where(x => x.BuildingId == buildingId.Value);
        }

        if (startDate.HasValue)
        {
            incomeQuery = incomeQuery.Where(x => x.PaymentDate >= startDate.Value);
            expenseQuery = expenseQuery.Where(x => x.ExpenseDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            incomeQuery = incomeQuery.Where(x => x.PaymentDate <= endDate.Value);
            expenseQuery = expenseQuery.Where(x => x.ExpenseDate <= endDate.Value);
        }

        var totalIncomes = incomeQuery.Where(x => x.Status == IncomeStatus.Paid).Sum(x => x.Amount);
        var totalExpenses = expenseQuery.Where(x => x.Status == ExpenseStatus.Paid).Sum(x => x.Amount);

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

        // Combined transaction ledger merging income + expense into one dated feed — unique to
        // Reports, no equivalent method exists on the income/expense services to delegate to.
        var incomeRecords = await _unitOfWork.IncomeRecordRepository.GetAllAsync();
        var expenseRecords = await _unitOfWork.ExpenseRecordRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);

        var incomeQuery = incomeRecords.AsQueryable();
        var expenseQuery = expenseRecords.AsQueryable();

        if (buildingId.HasValue)
        {
            incomeQuery = incomeQuery.Where(x => x.BuildingId == buildingId.Value);
            expenseQuery = expenseQuery.Where(x => x.BuildingId == buildingId.Value);
        }

        if (startDate.HasValue)
        {
            incomeQuery = incomeQuery.Where(x => x.PaymentDate >= startDate.Value);
            expenseQuery = expenseQuery.Where(x => x.ExpenseDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            incomeQuery = incomeQuery.Where(x => x.PaymentDate <= endDate.Value);
            expenseQuery = expenseQuery.Where(x => x.ExpenseDate <= endDate.Value);
        }

        var ledgerItems = new List<LedgerItem>();

        foreach (var inc in incomeQuery.ToList())
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

        foreach (var exp in expenseQuery.ToList())
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

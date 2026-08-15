using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Income;
using CleanArchitecture.Shared.Models.Expenses;
using CleanArchitecture.Shared.Models.Reports;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IReportService
{
    Task<PaginatedList<IncomeRecordViewModel>> GetIncomeReport(
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PaginatedList<ExpenseRecordViewModel>> GetExpenseReport(
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ReportStatsViewModel> GetReportStats(
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);

    Task<byte[]> ExportReportToXlsx(
        string type,
        Guid? buildingId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);
}

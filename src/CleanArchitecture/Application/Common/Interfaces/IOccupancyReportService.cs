using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Occupancy;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IOccupancyReportService
{
    Task<PaginatedList<OccupancyReportItemViewModel>> GetOccupancyReport(
        Guid? buildingId,
        DateTime fromDate,
        DateTime toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<OccupancyReportSummaryViewModel> GetOccupancyReportSummary(
        Guid? buildingId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken);

    Task<byte[]> ExportOccupancyReportToXlsx(
        Guid? buildingId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken);
}

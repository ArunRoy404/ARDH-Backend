using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Income;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IIncomeRecordService
{
    Task<PaginatedList<IncomeRecordViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        IncomeType? incomeType,
        IncomeStatus? status,
        Guid? buildingId,
        Guid? tenantId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);

    Task<IncomeRecordViewModel> GetById(Guid id, CancellationToken cancellationToken);

    Task Create(IncomeRecordCreateRequest request, CancellationToken cancellationToken);

    Task Update(Guid id, IncomeRecordUpdateRequest request, CancellationToken cancellationToken);

    Task Delete(Guid id, CancellationToken cancellationToken);

    Task UpdateStatus(Guid id, IncomeRecordStatusUpdateRequest request, CancellationToken cancellationToken);

    Task<byte[]> GenerateReceiptPdf(Guid id, CancellationToken cancellationToken);

    Task<byte[]> ExportToCsv(
        string? search,
        IncomeType? incomeType,
        IncomeStatus? status,
        Guid? buildingId,
        Guid? tenantId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);
}

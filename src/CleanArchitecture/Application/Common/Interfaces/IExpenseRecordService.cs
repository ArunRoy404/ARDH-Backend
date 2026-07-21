using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Expenses;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IExpenseRecordService
{
    Task<PaginatedList<ExpenseRecordViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        ExpenseCategory? category,
        ExpenseStatus? status,
        ExpenseNature? nature,
        Guid? buildingId,
        Guid? vendorId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);

    Task<ExpenseRecordViewModel> GetById(Guid id, CancellationToken cancellationToken);

    Task Create(ExpenseRecordCreateRequest request, CancellationToken cancellationToken);

    Task Update(Guid id, ExpenseRecordUpdateRequest request, CancellationToken cancellationToken);

    Task Delete(Guid id, CancellationToken cancellationToken);

    Task UpdateStatus(Guid id, ExpenseRecordStatusUpdateRequest request, CancellationToken cancellationToken);
}

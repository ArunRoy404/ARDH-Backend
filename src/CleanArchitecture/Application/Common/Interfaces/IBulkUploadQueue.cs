using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IBulkUploadQueue
{
    void Enqueue(Guid bulkUploadId);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;

namespace CleanArchitecture.Application.Services;

/// <summary>
/// In-memory, unbounded queue of bulk-upload job ids processed one at a time
/// by <see cref="BulkUploadBackgroundService"/>.
/// </summary>
public class BulkUploadQueue : IBulkUploadQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid bulkUploadId)
    {
        _channel.Writer.TryWrite(bulkUploadId);
    }

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}

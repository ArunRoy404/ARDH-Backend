using System;
using System.Collections.Generic;
using System.Threading;
using CleanArchitecture.Shared.Models.BulkUpload;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IBulkUploadProgressBroadcaster
{
    /// <summary>Publishes a real-time progress update event for a bulk upload track ID.</summary>
    void PublishProgress(BulkUploadProgressEvent progressEvent);

    /// <summary>Subscribes to real-time progress update events for a bulk upload track ID.</summary>
    IAsyncEnumerable<BulkUploadProgressEvent> SubscribeAsync(Guid trackId, CancellationToken cancellationToken);
}

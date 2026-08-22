using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.BulkUpload;

namespace CleanArchitecture.Application.Services;

public class BulkUploadProgressBroadcaster : IBulkUploadProgressBroadcaster
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Channel<BulkUploadProgressEvent>, byte>> _subscribers = new();

    public void PublishProgress(BulkUploadProgressEvent progressEvent)
    {
        if (_subscribers.TryGetValue(progressEvent.TrackId, out var channels))
        {
            foreach (var channel in channels.Keys)
            {
                channel.Writer.TryWrite(progressEvent);
            }
        }
    }

    public async IAsyncEnumerable<BulkUploadProgressEvent> SubscribeAsync(
        Guid trackId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<BulkUploadProgressEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var trackSubscribers = _subscribers.GetOrAdd(trackId, _ => new ConcurrentDictionary<Channel<BulkUploadProgressEvent>, byte>());
        trackSubscribers.TryAdd(channel, 0);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                BulkUploadProgressEvent progressEvent;
                try
                {
                    progressEvent = await channel.Reader.ReadAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                yield return progressEvent;

                if (progressEvent.Status is BulkUploadStatus.Finished or BulkUploadStatus.Failed || progressEvent.ProgressPercentage >= 100)
                {
                    break;
                }
            }
        }
        finally
        {
            trackSubscribers.TryRemove(channel, out _);
            if (trackSubscribers.IsEmpty)
            {
                _subscribers.TryRemove(trackId, out _);
            }
        }
    }
}

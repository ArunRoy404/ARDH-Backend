using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Services;

/// <summary>
/// Processes bulk-upload jobs one at a time from the in-memory queue.
/// Runs in the background so the API responds immediately after enqueueing.
/// </summary>
public class BulkUploadBackgroundService(
    IBulkUploadQueue bulkUploadQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<BulkUploadBackgroundService> logger) : BackgroundService
{
    private readonly IBulkUploadQueue _bulkUploadQueue = bulkUploadQueue;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<BulkUploadBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Re-queue any jobs that were left in "Processing" by a previous process (server
        // restart/crash while a job was running). Without this they would stay stuck forever,
        // since the in-memory queue is empty after a restart.
        await RecoverInterruptedJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var bulkUploadId = await _bulkUploadQueue.DequeueAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBulkUploadService>();
                await service.ProcessAsync(bulkUploadId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk upload background processing failed.");
            }
        }
    }

    private async Task RecoverInterruptedJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IBulkUploadService>();
            var interrupted = await service.GetInterruptedJobIdsAsync(stoppingToken);

            foreach (var id in interrupted)
            {
                _bulkUploadQueue.Enqueue(id);
            }

            if (interrupted.Count > 0)
            {
                _logger.LogInformation(
                    "Recovered {Count} interrupted bulk-upload job(s) left in 'Processing' by a previous process.",
                    interrupted.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover interrupted bulk-upload jobs on startup.");
        }
    }
}

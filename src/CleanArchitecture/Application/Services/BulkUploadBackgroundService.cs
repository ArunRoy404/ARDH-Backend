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
}

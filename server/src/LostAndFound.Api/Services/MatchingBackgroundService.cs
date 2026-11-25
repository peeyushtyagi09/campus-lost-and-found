namespace LostAndFound.Api.Services;

public sealed class MatchingBackgroundService : BackgroundService
{
    private readonly IMatchingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MatchingBackgroundService> _logger;

    public MatchingBackgroundService(
        IMatchingQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<MatchingBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

                if (job.IsFoundReport)
                {
                    await matchingService.MatchForFoundReportAsync(job.ReportId, stoppingToken);
                }
                else
                {
                    await matchingService.MatchForLostReportAsync(job.ReportId, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error computing matches for report {ReportId}.", job.ReportId);
            }
        }
    }
}

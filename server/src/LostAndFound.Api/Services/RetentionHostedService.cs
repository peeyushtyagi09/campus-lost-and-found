using LostAndFound.Api.Options;
using LostAndFound.Api.Repositories;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Services;

public sealed class RetentionHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval;
    private readonly int _expiredAccountDays;
    private readonly ILogger<RetentionHostedService> _logger;

    public RetentionHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RetentionOptions> retentionOptions,
        ILogger<RetentionHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _interval = TimeSpan.FromMinutes(Math.Max(retentionOptions.Value.SweepIntervalMinutes, 1));
        _expiredAccountDays = retentionOptions.Value.ExpiredAccountDays;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var disposalService = scope.ServiceProvider.GetRequiredService<IDisposalService>();
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var flagged = await disposalService.FlagExpiredFoundItemsAsync(cancellationToken);
            var accountCutoff = DateTime.UtcNow.AddDays(-_expiredAccountDays);
            var purged = await userRepo.DeleteExpiredStandardUsersAsync(accountCutoff, cancellationToken);

            if (flagged > 0 || purged > 0)
            {
                _logger.LogInformation(
                    "Retention sweep completed: flagged {FlaggedCount} found items, purged {PurgedCount} expired accounts.",
                    flagged,
                    purged);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during retention sweep.");
        }
    }
}

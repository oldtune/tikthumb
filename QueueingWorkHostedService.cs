
namespace tikthumb;
public class QueueingWorkHostedService : BackgroundService
{
    readonly ILogger<QueueingWorkHostedService> _logger;

    public QueueingWorkHostedService(ILogger<QueueingWorkHostedService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Executing");
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }
}
namespace Claims.Auditing;

public class AuditWorker : BackgroundService
{
    private readonly AuditQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditWorker> _logger;

    public AuditWorker(AuditQueue queue, IServiceScopeFactory scopeFactory, ILogger<AuditWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AuditContext>();

                if (message.Entity == AuditEntity.Claim)
                {
                    context.Add(new ClaimAudit
                    {
                        ClaimId = message.EntityId,
                        HttpRequestType = message.HttpRequestType,
                        Created = message.Created
                    });
                }
                else
                {
                    context.Add(new CoverAudit
                    {
                        CoverId = message.EntityId,
                        HttpRequestType = message.HttpRequestType,
                        Created = message.Created
                    });
                }

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to save audit for {Entity} {Id}", message.Entity, message.EntityId);
            }
        }
    }
}
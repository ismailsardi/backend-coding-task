namespace Claims.Auditing;

public class Auditer : IAuditer
{
    private readonly AuditQueue _queue;
    private readonly TimeProvider _timeProvider;

    public Auditer(AuditQueue queue, TimeProvider timeProvider)
    {
        _queue = queue;
        _timeProvider = timeProvider;
    }

    public void AuditClaim(string id, string httpRequestType)
    {
        Enqueue(AuditEntity.Claim, id, httpRequestType);
    }

    public void AuditCover(string id, string httpRequestType)
    {
        Enqueue(AuditEntity.Cover, id, httpRequestType);
    }

    private void Enqueue(AuditEntity entity, string id, string httpRequestType)
    {
        var message = new AuditMessage(entity, id, httpRequestType, _timeProvider.GetUtcNow().UtcDateTime);
        _queue.Writer.TryWrite(message);
    }
}
namespace Claims.Auditing;

public enum AuditEntity
{
    Claim,
    Cover
}

public record AuditMessage(AuditEntity Entity, string EntityId, string HttpRequestType, DateTime Created);
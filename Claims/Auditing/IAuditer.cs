namespace Claims.Auditing;

public interface IAuditer
{
    void AuditCover(string id, string httpRequestType);
    void AuditClaim(string id, string httpRequestType);
}
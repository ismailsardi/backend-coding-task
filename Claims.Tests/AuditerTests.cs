using Claims.Auditing;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Claims.Tests;

public class AuditerTests
{
    [Fact]
    public void AuditClaim_EnqueuesMessageWithUtcTimestamp()
    {
        var now = new DateTimeOffset(2027, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var queue = new AuditQueue();
        var auditer = new Auditer(queue, new FakeTimeProvider(now));

        auditer.AuditClaim("claim-1", "POST");

        Assert.True(queue.Reader.TryRead(out var message));
        Assert.Equal(new AuditMessage(AuditEntity.Claim, "claim-1", "POST", now.UtcDateTime), message);
    }
}
using System.Threading.Channels;

namespace Claims.Auditing;

public class AuditQueue
{
    private readonly Channel<AuditMessage> _channel = Channel.CreateUnbounded<AuditMessage>();

    public ChannelWriter<AuditMessage> Writer => _channel.Writer;
    public ChannelReader<AuditMessage> Reader => _channel.Reader;
}
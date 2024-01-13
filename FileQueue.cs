using System.Threading.Channels;

namespace tikthumb;
public class FileQueue
{
    readonly Channel<FileQueueItem> _channel;
    public FileQueue(Channel<FileQueueItem> channel)
    {
        _channel = channel;
    }

    public async Task Enqueue(FileQueueItem item)
    {
        await _channel.Writer.WriteAsync(item);
    }

    public async Task Dequeue()
    {
        await _channel.Reader.ReadAsync();
    }
}

public class FileQueueItem
{
    public string FileName { set; get; }
    public string CreatedAtUtc { set; get; }
    public string IsStreaming { set; get; }
}
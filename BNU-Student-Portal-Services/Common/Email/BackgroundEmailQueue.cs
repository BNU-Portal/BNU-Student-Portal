using System.Threading.Channels;

namespace BNU_Student_Portal_Services.Common.Behaviors.Email;

public class BackgroundEmailQueue
{
    private readonly Channel<Func<CancellationToken, Task>> _queue;

    public BackgroundEmailQueue()
    {
        _queue = Channel.CreateBounded<Func<CancellationToken, Task>>(100);
    }

    public async ValueTask EnqueueAsync(Func<CancellationToken, Task> job)
    {
        await _queue.Writer.WriteAsync(job);
    }

    public async ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }

}
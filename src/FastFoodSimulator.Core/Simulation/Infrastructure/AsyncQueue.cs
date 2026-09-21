using System.Threading.Channels;
using FastFoodSimulator.Core.Abstractions;

namespace FastFoodSimulator.Core.Simulation.Infrastructure;

/// <summary>Unbounded FIFO queue: producers never block, consumers asynchronously wait for items.</summary>
public sealed class AsyncQueue<T> : IWorkQueue<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    public void Enqueue(T item)
    {
        if (!_channel.Writer.TryWrite(item))
        {
            throw new InvalidOperationException("The queue does not accept new items.");
        }
    }

    public ValueTask<T> DequeueAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAsync(cancellationToken);
}

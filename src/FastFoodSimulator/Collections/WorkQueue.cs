using System.Collections.Concurrent;

namespace FastFoodSimulator.Collections;

public sealed class WorkQueue<T>
{
    private readonly ConcurrentQueue<T> _items = new();
    private readonly SemaphoreSlim _signal = new(0);

    public int Count => _items.Count;

    public void Enqueue(T item)
    {
        _items.Enqueue(item);
        _signal.Release();
    }

    public async Task<T> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _items.TryDequeue(out var item);
        return item!;
    }

    public T[] ToArray() => _items.ToArray();
}

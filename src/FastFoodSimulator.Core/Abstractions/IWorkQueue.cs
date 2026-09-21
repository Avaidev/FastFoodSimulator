namespace FastFoodSimulator.Core.Abstractions;

/// <summary>Asynchronous FIFO queue connecting two actors.</summary>
public interface IWorkQueue<T>
{
    void Enqueue(T item);

    ValueTask<T> DequeueAsync(CancellationToken cancellationToken);
}

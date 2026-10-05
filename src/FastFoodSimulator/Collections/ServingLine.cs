using System.Collections.Concurrent;
using FastFoodSimulator.Models;

namespace FastFoodSimulator.Collections;

public sealed class ServingLine
{
    private readonly object _gate = new();
    private ConcurrentQueue<Order> _queue = new();

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _queue.Count;
            }
        }
    }

    public void Enqueue(Order order)
    {
        lock (_gate)
        {
            _queue.Enqueue(order);
        }
    }

    public void Remove(Order order)
    {
        lock (_gate)
        {
            _queue = new ConcurrentQueue<Order>(_queue.Where(o => !ReferenceEquals(o, order)));
        }
    }

    public Order[] ToArray()
    {
        lock (_gate)
        {
            return _queue.ToArray();
        }
    }
}

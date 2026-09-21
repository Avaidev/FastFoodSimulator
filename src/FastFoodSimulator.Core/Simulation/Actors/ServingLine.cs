using FastFoodSimulator.Core.Abstractions;
using FastFoodSimulator.Core.Configuration;
using FastFoodSimulator.Core.Domain;
using FastFoodSimulator.Core.Simulation.Infrastructure;

namespace FastFoodSimulator.Core.Simulation.Actors;

/// <summary>
/// Customers waiting for their number. Every customer waits on his own pickup promise concurrently;
/// once it is completed the customer takes the order and leaves for the dining area.
/// </summary>
public sealed class ServingLine(SimulationSettings settings, IRestaurantBoardWriter board)
    : SimulationActorBase, IServingLine
{
    private readonly AsyncQueue<Customer> _arrivals = new();
    private readonly HashSet<Task> _waiting = new();
    private readonly object _gate = new();

    public void Join(Customer customer)
    {
        var order = customer.Order
            ?? throw new InvalidOperationException("A customer without an order cannot join the serving line.");

        board.CustomerJoinedServingLine(order.Number);
        _arrivals.Enqueue(customer);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var customer = await _arrivals.DequeueAsync(cancellationToken).ConfigureAwait(false);
                Track(WaitAndPickUpAsync(customer, cancellationToken));
            }
        }
        finally
        {
            Task[] pending;
            lock (_gate)
            {
                pending = _waiting.ToArray();
            }

            await Task.WhenAll(pending).ConfigureAwait(false);
        }
    }

    private async Task WaitAndPickUpAsync(Customer customer, CancellationToken cancellationToken)
    {
        try
        {
            await customer.WaitForOrderAsync(cancellationToken).ConfigureAwait(false);   // waits on the promise
            await Task.Delay(settings.PickupDuration, cancellationToken).ConfigureAwait(false);
            board.OrderPickedUp(customer.Order!.Number);                                   // -> dining area
        }
        catch (OperationCanceledException)
        {
            // Simulation stopped while the customer was waiting.
        }
    }

    private void Track(Task task)
    {
        lock (_gate)
        {
            _waiting.Add(task);
        }

        _ = task.ContinueWith(
            finished =>
            {
                lock (_gate)
                {
                    _waiting.Remove(finished);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}

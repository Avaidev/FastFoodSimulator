using FastFoodSimulator.Core.Abstractions;
using FastFoodSimulator.Core.Configuration;
using FastFoodSimulator.Core.Domain;

namespace FastFoodSimulator.Core.Simulation.Actors;

/// <summary>
/// Removes the next ticket from the kitchen queue, prepares the order (fixed interval),
/// completes the order's preparation promise and moves the finished order to the service queue.
/// </summary>
public sealed class Cook(
    SimulationSettings settings,
    IWorkQueue<Order> kitchenQueue,
    IWorkQueue<Order> serviceQueue,
    IRestaurantBoardWriter board) : SimulationActorBase
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var order = await kitchenQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);

            board.CookingStarted(order.Number);
            await Task.Delay(settings.CookingInterval, cancellationToken).ConfigureAwait(false);

            order.MarkPrepared();                // promise #1 – the Server is released for this ticket
            board.CookingFinished(order.Number);
            serviceQueue.Enqueue(order);         // completed order goes to the service queue (FIFO)
        }
    }
}

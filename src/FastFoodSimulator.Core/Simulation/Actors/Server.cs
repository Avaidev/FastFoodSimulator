using FastFoodSimulator.Core.Abstractions;
using FastFoodSimulator.Core.Configuration;
using FastFoodSimulator.Core.Domain;

namespace FastFoodSimulator.Core.Simulation.Actors;

/// <summary>
/// Removes the next completed order from the service queue, waits on the order's preparation promise,
/// calls out the order number and thereby notifies the customer that holds the same number.
/// </summary>
public sealed class Server(
    SimulationSettings settings,
    IWorkQueue<Order> serviceQueue,
    IRestaurantBoardWriter board) : SimulationActorBase
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var order = await serviceQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);

            // Promise #1: the Server never serves a ticket whose food is not marked as prepared.
            await order.PreparationPromise.WaitAsync(cancellationToken).ConfigureAwait(false);

            board.OrderCalledOut(order.Number);
            await Task.Delay(settings.AnnouncementDuration, cancellationToken).ConfigureAwait(false);

            order.MarkReadyForPickup();          // promise #2 – the customer may pick the order up
        }
    }
}

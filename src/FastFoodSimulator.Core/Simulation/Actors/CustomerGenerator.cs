using FastFoodSimulator.Core.Abstractions;
using FastFoodSimulator.Core.Configuration;
using FastFoodSimulator.Core.Domain;

namespace FastFoodSimulator.Core.Simulation.Actors;

/// <summary>Sends a new customer to the order line at a constant rate.</summary>
public sealed class CustomerGenerator(
    SimulationSettings settings,
    IWorkQueue<Customer> orderLine,
    IRestaurantBoardWriter board) : SimulationActorBase
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(settings.CustomerArrivalInterval);
        var nextCustomerId = 0;

        do
        {
            var customer = new Customer(++nextCustomerId);
            board.CustomerJoinedOrderLine(customer.Id);
            orderLine.Enqueue(customer);
        }
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
    }
}

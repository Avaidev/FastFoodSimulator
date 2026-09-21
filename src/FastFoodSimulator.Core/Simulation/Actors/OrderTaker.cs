using FastFoodSimulator.Core.Abstractions;
using FastFoodSimulator.Core.Configuration;
using FastFoodSimulator.Core.Domain;

namespace FastFoodSimulator.Core.Simulation.Actors;

/// <summary>
/// Takes orders one by one: creates the ticket with a unique number, hands the receipt to the customer,
/// sends the customer to the serving line and puts the ticket on the kitchen carousel (FIFO queue).
/// </summary>
public sealed class OrderTaker(
    SimulationSettings settings,
    IWorkQueue<Customer> orderLine,
    IWorkQueue<Order> kitchenQueue,
    IServingLine servingLine,
    IOrderNumberGenerator orderNumbers,
    IRestaurantBoardWriter board) : SimulationActorBase
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var customer = await orderLine.DequeueAsync(cancellationToken).ConfigureAwait(false);

            var order = new Order(orderNumbers.Next(), customer.Id);
            board.OrderTakingStarted(customer.Id, order.Number);

            await Task.Delay(settings.OrderTakingDuration, cancellationToken).ConfigureAwait(false);

            customer.ReceiveOrder(order);        // 1. receipt for the customer
            servingLine.Join(customer);          // 2. customer moves to the serving line
            board.OrderPlaced(order.Number);
            kitchenQueue.Enqueue(order);         // 3. ticket goes onto the kitchen carousel
        }
    }
}

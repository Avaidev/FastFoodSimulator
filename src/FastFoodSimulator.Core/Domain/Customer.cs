namespace FastFoodSimulator.Core.Domain;

/// <summary>A simulated customer: arrives, gets a receipt (order) and waits for the order number to be called.</summary>
public sealed class Customer
{
    public Customer(int id) => Id = id;

    public int Id { get; }

    /// <summary>The order the customer received after paying. <c>null</c> until the Order Taker is done.</summary>
    public Order? Order { get; private set; }

    public void ReceiveOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        Order = order;
    }

    /// <summary>Waits (in the serving line) until the Server calls out this customer's order number.</summary>
    public Task WaitForOrderAsync(CancellationToken cancellationToken)
    {
        if (Order is null)
        {
            throw new InvalidOperationException("The customer has not received an order yet.");
        }

        return Order.PickupPromise.WaitAsync(cancellationToken);
    }
}

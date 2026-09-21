namespace FastFoodSimulator.Core.Domain;

/// <summary>
/// An order ticket. It is represented by two promises (<see cref="TaskCompletionSource"/>):
/// <list type="bullet">
///   <item><see cref="PreparationPromise"/> – the Server waits on it while the Cook prepares the order.</item>
///   <item><see cref="PickupPromise"/> – the Customer waits on it while standing in the serving line.</item>
/// </list>
/// </summary>
public sealed class Order
{
    private readonly TaskCompletionSource _preparation = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _pickup = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Order(int number, int customerId)
    {
        Number = number;
        CustomerId = customerId;
    }

    /// <summary>Unique order number created by the Order Taker.</summary>
    public int Number { get; }

    public int CustomerId { get; }

    /// <summary>Completed by the Cook when the food is ready. Awaited by the Server.</summary>
    public Task PreparationPromise => _preparation.Task;

    /// <summary>Completed by the Server when the number is called out. Awaited by the Customer.</summary>
    public Task PickupPromise => _pickup.Task;

    public void MarkPrepared() => _preparation.TrySetResult();

    public void MarkReadyForPickup() => _pickup.TrySetResult();
}

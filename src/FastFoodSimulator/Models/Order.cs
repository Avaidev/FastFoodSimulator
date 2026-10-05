namespace FastFoodSimulator.Models;

public sealed class Order
{
    private int _state;

    public Order(int id, int customerId)
    {
        Id = id;
        CustomerId = customerId;
        KitchenPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CustomerPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public int Id { get; }

    public int CustomerId { get; }

    public OrderState State
    {
        get => (OrderState)Volatile.Read(ref _state);
        set => Volatile.Write(ref _state, (int)value);
    }

    public TaskCompletionSource KitchenPromise { get; }

    public TaskCompletionSource CustomerPromise { get; }
}

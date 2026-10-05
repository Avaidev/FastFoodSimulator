namespace FastFoodSimulator.Models;

public sealed class Customer
{
    public Customer(int id)
    {
        Id = id;
        Receipt = new TaskCompletionSource<Order>(TaskCreationOptions.RunContinuationsAsynchronously);
        InServingLine = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public int Id { get; }

    public TaskCompletionSource<Order> Receipt { get; }

    public TaskCompletionSource InServingLine { get; }
}

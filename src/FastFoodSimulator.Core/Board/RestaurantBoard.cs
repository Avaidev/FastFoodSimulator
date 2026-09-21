using FastFoodSimulator.Core.Abstractions;

namespace FastFoodSimulator.Core.Board;

/// <summary>
/// Thread-safe, observable state of the restaurant. Actors write to it, the UI reads snapshots from it.
/// Every change produces a snapshot with a growing <see cref="RestaurantSnapshot.Version"/>,
/// so a consumer can drop out-of-order notifications.
/// </summary>
public sealed class RestaurantBoard : IRestaurantBoard
{
    private readonly object _gate = new();
    private readonly List<int> _orderLine = new();
    private readonly List<int> _kitchenQueue = new();
    private readonly List<int> _servingLine = new();
    private readonly List<int> _readyOnCounter = new();
    private int? _orderBeingTaken;
    private int? _orderBeingCooked;
    private long _version;
    private RestaurantSnapshot _current = RestaurantSnapshot.Empty;

    public event EventHandler<RestaurantSnapshot>? Changed;

    public RestaurantSnapshot Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void Reset() => Mutate(() =>
    {
        _orderLine.Clear();
        _kitchenQueue.Clear();
        _servingLine.Clear();
        _readyOnCounter.Clear();
        _orderBeingTaken = null;
        _orderBeingCooked = null;
    });

    public void CustomerJoinedOrderLine(int customerId) => Mutate(() => _orderLine.Add(customerId));

    public void OrderTakingStarted(int customerId, int orderNumber) => Mutate(() =>
    {
        _orderLine.Remove(customerId);
        _orderBeingTaken = orderNumber;
    });

    public void OrderPlaced(int orderNumber) => Mutate(() =>
    {
        if (_orderBeingTaken == orderNumber)
        {
            _orderBeingTaken = null;
        }

        _kitchenQueue.Add(orderNumber);
    });

    public void CookingStarted(int orderNumber) => Mutate(() =>
    {
        _kitchenQueue.Remove(orderNumber);
        _orderBeingCooked = orderNumber;
    });

    public void CookingFinished(int orderNumber) => Mutate(() =>
    {
        if (_orderBeingCooked == orderNumber)
        {
            _orderBeingCooked = null;
        }
    });

    public void CustomerJoinedServingLine(int orderNumber) => Mutate(() => _servingLine.Add(orderNumber));

    public void OrderCalledOut(int orderNumber) => Mutate(() => _readyOnCounter.Add(orderNumber));

    public void OrderPickedUp(int orderNumber) => Mutate(() =>
    {
        _readyOnCounter.Remove(orderNumber);
        _servingLine.Remove(orderNumber);
    });

    private void Mutate(Action change)
    {
        RestaurantSnapshot snapshot;

        lock (_gate)
        {
            change();
            snapshot = _current = BuildSnapshot();
        }

        Changed?.Invoke(this, snapshot);
    }

    private RestaurantSnapshot BuildSnapshot() => new(
        ++_version,
        _orderLine.ToArray(),
        _orderBeingTaken,
        _orderBeingCooked,
        _kitchenQueue.ToArray(),
        _servingLine.ToArray(),
        _readyOnCounter.ToArray());
}
